using Markov.Services.Enums;
using Markov.Services.Indicators;
using Markov.Services.Models;

namespace Markov.Services.Filters;

public class TrendFilter : ISignalFilter
{
    private readonly int _longTermMAPeriod;
    private readonly SmaIndicator _smaIndicator;

    public TrendFilter(int longTermMAPeriod = 200)
    {
        _longTermMAPeriod = longTermMAPeriod;
        _smaIndicator = new SmaIndicator(_longTermMAPeriod);
    }

    public string Name => $"TrendFilter({_longTermMAPeriod})";

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
            var smaValues = _smaIndicator.Calculate(candles).ToList();
            var signalCandleIndex = candles.FindIndex(c => c.Timestamp == signal.Timestamp);

            if (signalCandleIndex == -1 || signalCandleIndex >= smaValues.Count)
            {
                continue; // Not enough data to calculate SMA for this signal
            }

            decimal currentPrice = candles[signalCandleIndex].Close;
            decimal maValue = smaValues[signalCandleIndex];

            // Rule: Only allow buy signals if the price is above the long-term moving average (uptrend).
            if (signal.Type == SignalType.Buy && currentPrice < maValue)
            {
                continue; // Filter out buy signal in a downtrend
            }

            // Rule: Only allow sell signals if the price is below the long-term moving average (downtrend).
            if (signal.Type == SignalType.Sell && currentPrice > maValue)
            {
                continue; // Filter out sell signal in an uptrend
            }

            filteredSignals.Add(signal);
        }

        return filteredSignals;
    }
}