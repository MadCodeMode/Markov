using Markov.Services.Interfaces;
using Markov.Services.Models;
using Markov.Services.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Markov.Services.Engine
{
    public class BacktestingEngine : IBacktestingEngine
    {
        private const decimal MinimumCapitalToOpenPosition = 10m;

        public async Task<BacktestResult> RunAsync(IStrategy strategy, BacktestParameters parameters)
        {
            var data = await parameters.Exchange.GetHistoricalDataAsync(parameters.Symbol, parameters.TimeFrame, parameters.From, parameters.To, CancellationToken.None);
            var candles = data.ToList();
            if (!candles.Any())
            {
                return new BacktestResult { InitialCapital = parameters.InitialCapital, FinalCapital = parameters.InitialCapital };
            }

            var signals = strategy.GetFilteredSignals(new Dictionary<string, IEnumerable<Candle>> { { parameters.Symbol, candles } }).ToList();

            var result = new BacktestResult
            {
                InitialCapital = parameters.InitialCapital,
                Trades = new List<Trade>()
            };

            var tradingCapital = parameters.InitialCapital;
            var openPositions = new List<Trade>();
            var heldAssets = new Dictionary<string, decimal>();

            for (int i = 0; i < candles.Count; i++)
            {
                var currentCandle = candles[i];

                // Process exits based on the current candle's data
                ProcessExits(openPositions, currentCandle, ref tradingCapital, result, parameters);

                // Process entries only if there is a next candle
                if (i < candles.Count - 1)
                {
                    var nextCandle = candles[i + 1];
                    var signalForThisCandle = signals.FirstOrDefault(s => s.Timestamp == currentCandle.Timestamp);
                    if (signalForThisCandle != null)
                    {
                        var entryPrice = nextCandle.Open; // Corrected entry price

                        if (openPositions.Any(p => p.Symbol == signalForThisCandle.Symbol))
                        {
                            var positionToClose = openPositions.First(p => p.Symbol == signalForThisCandle.Symbol);
                            if (positionToClose.Side != (signalForThisCandle.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell))
                            {
                                ClosePosition(positionToClose, entryPrice, nextCandle.Timestamp, TradeOutcome.Closed, ref tradingCapital, result, parameters);
                                openPositions.Remove(positionToClose);
                            }
                        }
                        else if (tradingCapital > MinimumCapitalToOpenPosition)
                        {
                            OpenPosition(signalForThisCandle, entryPrice, nextCandle.Timestamp, ref tradingCapital, openPositions, heldAssets, result, parameters);
                        }
                    }
                }
            }

            // Liquidate any remaining open positions at the close of the last candle
            for (int j = openPositions.Count - 1; j >= 0; j--)
            {
                var position = openPositions[j];
                var lastCandle = candles.Last();
                ClosePosition(position, lastCandle.Close, lastCandle.Timestamp, TradeOutcome.Closed, ref tradingCapital, result, parameters);
                openPositions.RemoveAt(j);
            }
            result.FinalCapital = tradingCapital;

            result.HeldAssets = heldAssets;
            if (candles.Any() && heldAssets.Any())
            {
                var lastPrice = candles.Last().Close;
                result.FinalHeldAssetsValue = heldAssets.Sum(kvp => kvp.Value * lastPrice);
            }
            result.RealizedPnl = result.Trades.Sum(t => t.Pnl);
            return result;
        }

        private void ProcessExits(List<Trade> openPositions, Candle currentCandle, ref decimal tradingCapital, BacktestResult result, BacktestParameters parameters)
        {
            for (int j = openPositions.Count - 1; j >= 0; j--)
            {
                var position = openPositions[j];
                bool positionClosed = false;

                if (position.Side == OrderSide.Buy)
                {
                    if (position.StopLoss.HasValue && currentCandle.Low <= position.StopLoss.Value)
                    {
                        ClosePosition(position, position.StopLoss.Value, currentCandle.Timestamp, TradeOutcome.StopLoss, ref tradingCapital, result, parameters);
                        positionClosed = true;
                    }
                    else if (position.TakeProfit.HasValue && currentCandle.High >= position.TakeProfit.Value)
                    {
                        ClosePosition(position, position.TakeProfit.Value, currentCandle.Timestamp, TradeOutcome.TakeProfit, ref tradingCapital, result, parameters);
                        positionClosed = true;
                    }
                }
                else // Sell side
                {
                    if (position.StopLoss.HasValue && currentCandle.High >= position.StopLoss.Value)
                    {
                        ClosePosition(position, position.StopLoss.Value, currentCandle.Timestamp, TradeOutcome.StopLoss, ref tradingCapital, result, parameters);
                        positionClosed = true;
                    }
                    else if (position.TakeProfit.HasValue && currentCandle.Low <= position.TakeProfit.Value)
                    {
                        ClosePosition(position, position.TakeProfit.Value, currentCandle.Timestamp, TradeOutcome.TakeProfit, ref tradingCapital, result, parameters);
                        positionClosed = true;
                    }
                }

                if (positionClosed)
                {
                    openPositions.RemoveAt(j);
                }
            }
        }


        private void OpenPosition(Signal signal, decimal entryPrice, DateTime entryTimestamp, ref decimal capital, List<Trade> openPositions, Dictionary<string, decimal> heldAssets, BacktestResult result, BacktestParameters parameters)
        {
            // 1. Determine the principal amount to invest
            decimal principalAmount;
            if (parameters.TradeSizeMode == TradeSizeMode.PercentageOfCapital)
            {
                principalAmount = capital * parameters.TradeSizeValue;
            }
            else
            {
                principalAmount = parameters.TradeSizeValue;
            }

            var side = signal.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell;

            // 2. Check for sufficient capital and scale down if necessary
            if (side == OrderSide.Buy)
            {
                decimal requiredCapital = principalAmount * (1 + parameters.CommissionPercentage);
                if (requiredCapital > capital)
                {
                    // Not enough capital, scale down to use all available capital.
                    principalAmount = capital / (1 + parameters.CommissionPercentage);
                }
            }
            else // OrderSide.Sell
            {
                // For short selling, margin is required. We'll use a simplified model
                // where the margin required is the principal amount of the trade.
                if (principalAmount > capital)
                {
                    principalAmount = capital; // Scale down to available capital (margin)
                }
            }

            // 3. Calculate trade details
            var actualEntryPrice = ApplySlippage(entryPrice, side, parameters.SlippagePercentage);
            var quantity = principalAmount / actualEntryPrice;
            decimal entryCommission = principalAmount * parameters.CommissionPercentage;

            // 4. Update capital
            if (side == OrderSide.Buy)
            {
                capital -= (principalAmount + entryCommission);
            }
            else // OrderSide.Sell
            {
                capital += principalAmount - entryCommission;
            }

            // 5. Record the new position
            if (signal.UseHoldStrategy && side == OrderSide.Buy)
            {
                if (!heldAssets.ContainsKey(signal.Symbol))
                {
                    heldAssets[signal.Symbol] = 0;
                }
                heldAssets[signal.Symbol] += quantity;
                result.HoldCount++;
            }
            else
            {
                openPositions.Add(new Trade
                {
                    Side = side,
                    Symbol = signal.Symbol,
                    Quantity = quantity,
                    EntryPrice = actualEntryPrice,
                    EntryTimestamp = entryTimestamp,
                    TakeProfit = signal.TakeProfit,
                    StopLoss = signal.StopLoss,
                    AmountInvested = principalAmount
                });
            }
        }

        private void ClosePosition(Trade position, decimal exitPrice, DateTime exitTimestamp, TradeOutcome outcome, ref decimal capital, BacktestResult result, BacktestParameters parameters)
        {
            var actualExitPrice = ApplySlippage(exitPrice, position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy, parameters.SlippagePercentage);

            position.ExitPrice = actualExitPrice;
            position.ExitTimestamp = exitTimestamp;
            position.Outcome = outcome;

            decimal amountInvested = position.AmountInvested;
            decimal valueOnExit = actualExitPrice * position.Quantity;

            // Calculate commissions for PNL reporting
            decimal entryCommission = amountInvested * parameters.CommissionPercentage;
            decimal exitCommission = valueOnExit * parameters.CommissionPercentage;

            // Calculate the net PNL for this trade and store it
            decimal grossPnl = (position.Side == OrderSide.Buy) ? valueOnExit - amountInvested : amountInvested - valueOnExit;
            position.Pnl = grossPnl - entryCommission - exitCommission;

            // Update capital based on the position side
            if (position.Side == OrderSide.Buy)
            {
                // We sold our long position, so we receive the proceeds.
                capital += valueOnExit - exitCommission;
            }
            else // OrderSide.Sell
            {
                // We bought to cover our short position, so we pay the cost.
                capital -= (valueOnExit + exitCommission);
            }

            if (position.Pnl > 0) result.WinCount++;
            else result.LossCount++;

            result.Trades.Add(position);
        }

        private decimal ApplySlippage(decimal price, OrderSide side, decimal slippagePercentage)
        {
            if (side == OrderSide.Buy)
            {
                return price * (1 + slippagePercentage);
            }
            else // Sell
            {
                return price * (1 - slippagePercentage);
            }
        }
    }
}