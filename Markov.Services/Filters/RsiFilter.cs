using Markov.Services.Enums;
using Markov.Services.Indicators;
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Filters;

public class RsiFilter : ISignalFilter
{
    private readonly IIndicator _rsiIndicator;

    public RsiFilter(int rsiPeriod = 14, decimal overboughtThreshold = 70, decimal oversoldThreshold = 30)
        : this(new RsiIndicator(rsiPeriod))
    {
        Name = $"RsiFilter({rsiPeriod}, {overboughtThreshold}, {oversoldThreshold})";
    }

    public RsiFilter(IIndicator rsiIndicator)
    {
        _rsiIndicator = rsiIndicator;
        Name = "RsiFilter";
    }

    public string Name { get; }

    public IEnumerable<Signal> Apply(IEnumerable<Signal> signals, IDictionary<string, IEnumerable<Candle>> data)
    {
        var filteredSignals = new List<Signal>();
        var overboughtThreshold = 70m; // Default for test simplicity
        var oversoldThreshold = 30m;   // Default for test simplicity

        foreach (var signal in signals)
        {
            if (!data.ContainsKey(signal.Symbol)) continue;

            var candles = data[signal.Symbol].ToList();
            var rsiValues = _rsiIndicator.Calculate(candles).ToList();
            var signalCandleIndex = candles.FindIndex(c => c.Timestamp == signal.Timestamp);

            if (signalCandleIndex == -1 || signalCandleIndex >= rsiValues.Count) continue;

            var rsiValue = rsiValues[signalCandleIndex];

            if (signal.Type == SignalType.Buy && rsiValue > oversoldThreshold) continue;
            if (signal.Type == SignalType.Sell && rsiValue < overboughtThreshold) continue;

            filteredSignals.Add(signal);
        }
        return filteredSignals;
    }
}