using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Engine;

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

            var signalForThisCandle = signals.FirstOrDefault(s => s.Timestamp == candle.Timestamp);
            if (signalForThisCandle != null)
            {
                if (openPositions.Any())
                {
                    var existingPosition = openPositions.First();
                    if (existingPosition.Side == OrderSide.Buy && signalForThisCandle.Type == SignalType.Sell ||
                        existingPosition.Side == OrderSide.Sell && signalForThisCandle.Type == SignalType.Buy)
                    {
                        ClosePosition(existingPosition, candle.Close, candle.Timestamp, TradeOutcome.Closed, ref tradingCapital, result);
                        openPositions.Clear();
                    }
                }

                if (tradingCapital > 0)
                {
                    var tradeSize = tradingCapital;
                    var quantity = tradeSize / candle.Close;

                    if (signalForThisCandle.UseHoldStrategy && signalForThisCandle.Type == SignalType.Buy)
                    {
                        if (!heldAssets.ContainsKey(signalForThisCandle.Symbol)) heldAssets[signalForThisCandle.Symbol] = 0;
                        heldAssets[signalForThisCandle.Symbol] += quantity;
                        tradingCapital -= tradeSize;
                        result.HoldCount++;
                    }
                    else
                    {
                        var newPosition = new Trade
                        {
                            Symbol = signalForThisCandle.Symbol,
                            Side = signalForThisCandle.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell,
                            Quantity = quantity,
                            EntryPrice = candle.Close,
                            EntryTimestamp = candle.Timestamp,
                            StopLoss = signalForThisCandle.StopLoss,
                            TakeProfit = signalForThisCandle.TakeProfit
                        };
                        openPositions.Add(newPosition);
                    }
                }
            }
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

        decimal pnl = 0;
        if (position.Side == OrderSide.Buy)
        {
            pnl = (exitPrice - position.EntryPrice) * position.Quantity;
        }
        else
        {
            pnl = (position.EntryPrice - exitPrice) * position.Quantity;
        }

        position.Pnl = pnl;
        capital += pnl;

        if (pnl > 0) result.WinCount++;
        else result.LossCount++;

        result.Trades.Add(position);
    }
}