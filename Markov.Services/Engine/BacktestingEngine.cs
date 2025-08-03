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
            var data = await parameters.Exchange.GetHistoricalDataAsync(parameters.Symbol, parameters.TimeFrame, parameters.From, parameters.To);
            var candles = data.ToList();
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
                var candle = candles[i];

                // --- FIX: Process new signals FIRST for the current candle ---
                var signalForThisCandle = signals.FirstOrDefault(s => s.Timestamp == candle.Timestamp);
                if (signalForThisCandle != null)
                {
                    if (openPositions.Any(p => p.Symbol == signalForThisCandle.Symbol))
                    {
                        var positionToClose = openPositions.First(p => p.Symbol == signalForThisCandle.Symbol);
                        if (positionToClose.Side != (signalForThisCandle.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell))
                        {
                             ClosePosition(positionToClose, signalForThisCandle.Price, candle.Timestamp, TradeOutcome.Closed, ref tradingCapital, result);
                             openPositions.Remove(positionToClose);
                        }
                    }

                    if (tradingCapital > 0 && !openPositions.Any(p=> p.Symbol == signalForThisCandle.Symbol))
                    {
                        var tradeSize = tradingCapital;
                        var quantity = tradeSize / signalForThisCandle.Price;

                        if (signalForThisCandle.UseHoldStrategy && signalForThisCandle.Type == SignalType.Buy)
                        {
                            if (!heldAssets.ContainsKey(signalForThisCandle.Symbol))
                            {
                                heldAssets[signalForThisCandle.Symbol] = 0;
                            }
                            heldAssets[signalForThisCandle.Symbol] += quantity;
                            tradingCapital = 0;
                            result.HoldCount++;
                        }
                        else
                        {
                           openPositions.Add(new Trade
                           {
                               Side = signalForThisCandle.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell,
                               Symbol = signalForThisCandle.Symbol,
                               Quantity = quantity,
                               EntryPrice = signalForThisCandle.Price,
                               EntryTimestamp = signalForThisCandle.Timestamp,
                               TakeProfit = signalForThisCandle.TakeProfit,
                               StopLoss = signalForThisCandle.StopLoss
                           });
                           tradingCapital = 0;
                        }
                    }
                }

                // --- Now, check for TP/SL hits on ANY open position, including one just opened ---
                for (int j = openPositions.Count - 1; j >= 0; j--)
                {
                    var position = openPositions[j];
                    bool positionClosed = false;

                    if (position.Side == OrderSide.Buy)
                    {
                        if (position.TakeProfit.HasValue && candle.High >= position.TakeProfit.Value)
                        {
                            ClosePosition(position, position.TakeProfit.Value, candle.Timestamp, TradeOutcome.TakeProfit, ref tradingCapital, result);
                            positionClosed = true;
                        }
                        else if (position.StopLoss.HasValue && candle.Low <= position.StopLoss.Value)
                        {
                            ClosePosition(position, position.StopLoss.Value, candle.Timestamp, TradeOutcome.StopLoss, ref tradingCapital, result);
                            positionClosed = true;
                        }
                    }
                    else
                    {
                        if (position.TakeProfit.HasValue && candle.Low <= position.TakeProfit.Value)
                        {
                            ClosePosition(position, position.TakeProfit.Value, candle.Timestamp, TradeOutcome.TakeProfit, ref tradingCapital, result);
                            positionClosed = true;
                        }
                        else if (position.StopLoss.HasValue && candle.High >= position.StopLoss.Value)
                        {
                            ClosePosition(position, position.StopLoss.Value, candle.Timestamp, TradeOutcome.StopLoss, ref tradingCapital, result);
                            positionClosed = true;
                        }
                    }

                    if (positionClosed)
                    {
                        openPositions.RemoveAt(j);
                    }
                }
            }

            if (openPositions.Any() && candles.Any())
            {
                var lastPrice = candles.Last().Close;
                tradingCapital += openPositions.Sum(p => p.Quantity * lastPrice);
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

        private void ClosePosition(Trade position, decimal exitPrice, DateTime exitTimestamp, TradeOutcome outcome, ref decimal capital, BacktestResult result)
        {
            position.ExitPrice = exitPrice;
            position.ExitTimestamp = exitTimestamp;
            position.Outcome = outcome;

            decimal initialValue = position.EntryPrice * position.Quantity;
            decimal finalValue = exitPrice * position.Quantity;
            decimal pnl = (position.Side == OrderSide.Buy) ? finalValue - initialValue : initialValue - finalValue;

            position.Pnl = pnl;
            
            // The free 'capital' is now the total value returned from the closed trade.
            capital = finalValue;

            if (pnl > 0) result.WinCount++;
            else result.LossCount++;

            result.Trades.Add(position);
        }
    }
}