using Markov.Services.Enums;
using Markov.Services.Filters;
using Markov.Services.Models;

namespace Markov.Trading.Engine.Filters;

public class TakeProfitStopLossFilter : ISignalFilter
{
    private readonly decimal _takeProfitPercentage;
    private readonly decimal _stopLossPercentage;
    private readonly bool _useHoldStrategyForLongs;

    public TakeProfitStopLossFilter(decimal takeProfitPercentage, decimal stopLossPercentage, bool useHoldStrategyForLongs = false)
    {
        _takeProfitPercentage = takeProfitPercentage;
        _stopLossPercentage = stopLossPercentage;
        _useHoldStrategyForLongs = useHoldStrategyForLongs;
    }

    public string Name => $"TP_SL_Filter({_takeProfitPercentage}%, {_stopLossPercentage}%)";

    public IEnumerable<Signal> Apply(IEnumerable<Signal> signals, IDictionary<string, IEnumerable<Candle>> data)
    {
        foreach (var signal in signals)
        {
            // This filter only applies SL/TP if they are not already set.
            // This allows other filters (like an ATR filter) to take precedence.
            if (signal.StopLoss.HasValue || signal.TakeProfit.HasValue)
            {
                continue;
            }

            if (signal.Type == SignalType.Buy)
            {
                signal.TakeProfit = signal.Price * (1 + _takeProfitPercentage);
                if (_useHoldStrategyForLongs)
                {
                    signal.UseHoldStrategy = true;
                    signal.StopLoss = null; // No stop loss when holding
                }
                else
                {
                    signal.StopLoss = signal.Price * (1 - _stopLossPercentage);
                }
            }
            else if (signal.Type == SignalType.Sell)
            {
                signal.TakeProfit = signal.Price * (1 - _takeProfitPercentage);
                signal.StopLoss = signal.Price * (1 + _stopLossPercentage);
            }
        }

        return signals;
    }
}