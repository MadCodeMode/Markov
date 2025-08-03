using Markov.Services.Enums;
using Markov.Services.Indicators;
using Markov.Services.Models;

namespace Markov.Services.Filters;

public class RsiFilter : ISignalFilter
{
    private readonly int _rsiPeriod;
    private readonly decimal _overboughtThreshold;
    private readonly decimal _oversoldThreshold;
    private readonly RsiIndicator _rsiIndicator;

    public RsiFilter(int rsiPeriod = 14, decimal overboughtThreshold = 70, decimal oversoldThreshold = 30)
    {
        _rsiPeriod = rsiPeriod;
        _overboughtThreshold = overboughtThreshold;
        _oversoldThreshold = oversoldThreshold;
        _rsiIndicator = new RsiIndicator(_rsiPeriod);
    }

    public string Name => $"RsiFilter({_rsiPeriod}, {_overboughtThreshold}, {_oversoldThreshold})";

    public IEnumerable<Signal> Apply(IEnumerable<Signal> signals, IDictionary<string, IEnumerable<Candle>> data)
    {
        var filteredSignals = new List<Signal>();

        foreach (var signal in signals)
        {
            if (!data.ContainsKey(signal.Symbol))
            {
                continue;
            }

            var candles = data[signal.Symbol].ToList();
            var rsiValues = _rsiIndicator.Calculate(candles).ToList();
            var signalCandleIndex = candles.FindIndex(c => c.Timestamp == signal.Timestamp);

            if (signalCandleIndex == -1 || signalCandleIndex >= rsiValues.Count)
            {
                continue; // Not enough data for RSI calculation
            }

            var rsiValue = rsiValues[signalCandleIndex];

            // Rule: Only allow buy signals if RSI is below the oversold threshold.
            if (signal.Type == SignalType.Buy && rsiValue > _oversoldThreshold)
            {
                continue;
            }

            // Rule: Only allow sell signals if RSI is above the overbought threshold.
            if (signal.Type == SignalType.Sell && rsiValue < _overboughtThreshold)
            {
                continue;
            }

            filteredSignals.Add(signal);
        }
        return filteredSignals;
    }
}