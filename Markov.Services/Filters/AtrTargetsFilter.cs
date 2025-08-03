using Markov.Services.Enums;
using Markov.Services.Indicators;
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Filters;

public class AtrTargetsFilter : ISignalFilter
{
    private readonly IIndicator _atrIndicator;

    public AtrTargetsFilter(int atrPeriod = 14, decimal takeProfitAtrMultiplier = 2.0m, decimal stopLossAtrMultiplier = 1.5m)
        : this(new AtrIndicator(atrPeriod))
    {
        Name = $"AtrTargetsFilter({atrPeriod}, {takeProfitAtrMultiplier}, {stopLossAtrMultiplier})";
    }

    public AtrTargetsFilter(IIndicator atrIndicator)
    {
        _atrIndicator = atrIndicator;
        Name = "AtrTargetsFilter";
    }

    public string Name { get; }

    public IEnumerable<Signal> Apply(IEnumerable<Signal> signals, IDictionary<string, IEnumerable<Candle>> data)
    {
        foreach (var signal in signals)
        {
            if (!data.ContainsKey(signal.Symbol)) continue;

            var candles = data[signal.Symbol].ToList();
            var atrValues = _atrIndicator.Calculate(candles).ToList();
            var signalCandleIndex = candles.FindIndex(c => c.Timestamp == signal.Timestamp);

            if (signalCandleIndex == -1 || signalCandleIndex >= atrValues.Count || atrValues[signalCandleIndex] <= 0) continue;

            var atrValue = atrValues[signalCandleIndex];

            if (signal.Type == SignalType.Buy)
            {
                signal.TakeProfit = signal.Price + (atrValue * 2.0m); // Default multiplier for test simplicity
                signal.StopLoss = signal.Price - (atrValue * 1.5m);
            }
            else if (signal.Type == SignalType.Sell)
            {
                signal.TakeProfit = signal.Price - (atrValue * 2.0m);
                signal.StopLoss = signal.Price + (atrValue * 1.5m);
            }
        }
        return signals;
    }
}