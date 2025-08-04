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
                        else if (tradingCapital > 10)
                        {
                            OpenPosition(signalForThisCandle, entryPrice, nextCandle.Timestamp, ref tradingCapital, openPositions, heldAssets, result, parameters);
                        }
                    }
                }
            }

            // Liquidate any remaining open positions at the close of the last candle
            if (openPositions.Any())
            {
                var lastPrice = candles.Last().Close;
                foreach (var position in openPositions)
                {
                    decimal finalValue = position.Quantity * lastPrice;
                    tradingCapital += finalValue;
                    tradingCapital -= finalValue * parameters.CommissionPercentage; // Commission on liquidation
                }
                openPositions.Clear(); // All positions are liquidated
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
            decimal tradeSize;
            if (parameters.TradeSizeMode == TradeSizeMode.PercentageOfCapital)
            {
                tradeSize = capital * parameters.TradeSizeValue;
            }
            else
            {
                tradeSize = parameters.TradeSizeValue;
            }

            if (tradeSize > capital)
            {
                tradeSize = capital;
            }

            var actualEntryPrice = ApplySlippage(entryPrice, signal.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell, parameters.SlippagePercentage);
            var commission = tradeSize * parameters.CommissionPercentage;
            var quantity = tradeSize / actualEntryPrice;

            capital -= (tradeSize + commission);

            if (signal.UseHoldStrategy && signal.Type == SignalType.Buy)
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
                    Side = signal.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell,
                    Symbol = signal.Symbol,
                    Quantity = quantity,
                    EntryPrice = actualEntryPrice,
                    EntryTimestamp = entryTimestamp,
                    TakeProfit = signal.TakeProfit,
                    StopLoss = signal.StopLoss,
                    AmountInvested = tradeSize
                });
            }
        }

        private void ClosePosition(Trade position, decimal exitPrice, DateTime exitTimestamp, TradeOutcome outcome, ref decimal capital, BacktestResult result, BacktestParameters parameters)
        {
            var actualExitPrice = ApplySlippage(exitPrice, position.Side == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy, parameters.SlippagePercentage);

            position.ExitPrice = actualExitPrice;
            position.ExitTimestamp = exitTimestamp;
            position.Outcome = outcome;

            decimal initialValue = position.AmountInvested;
            decimal finalValue = actualExitPrice * position.Quantity;
            decimal entryCommission = initialValue * parameters.CommissionPercentage;
            decimal exitCommission = finalValue * parameters.CommissionPercentage;
            decimal pnl = (position.Side == OrderSide.Buy) ? finalValue - initialValue : initialValue - finalValue;

            position.Pnl = pnl - entryCommission - exitCommission; // Net PNL

            capital += finalValue - exitCommission;

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