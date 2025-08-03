using Markov.Services.Indicators;
using Markov.Services.Models;

namespace Markov.Services.Filters;

public class VolumeFilter : ISignalFilter
{
    private readonly int _volumeMAPeriod;
    private readonly decimal _minVolumeMultiplier;
    private readonly SmaIndicator _volumeMaIndicator;


    public VolumeFilter(int volumeMAPeriod = 20, decimal minVolumeMultiplier = 1.5m)
    {
        _volumeMAPeriod = volumeMAPeriod;
        _minVolumeMultiplier = minVolumeMultiplier;
        _volumeMaIndicator = new SmaIndicator(_volumeMAPeriod);
    }

    public string Name => $"VolumeFilter({_volumeMAPeriod}, {_minVolumeMultiplier})";

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
            var volumeMaValues = _volumeMaIndicator.Calculate(candles.Select(c => new Candle { Close = c.Volume })).ToList();
            var signalCandleIndex = candles.FindIndex(c => c.Timestamp == signal.Timestamp);

            if (signalCandleIndex == -1 || signalCandleIndex >= volumeMaValues.Count)
            {
                continue; // Not enough data for Volume MA calculation
            }

            var volumeValue = candles[signalCandleIndex].Volume;
            var maVolumeValue = volumeMaValues[signalCandleIndex];

            // Rule: Only allow signals if the volume is significantly higher than average.
            if (volumeValue < maVolumeValue * _minVolumeMultiplier)
            {
                continue;
            }

            filteredSignals.Add(signal);
        }

        return filteredSignals;
    }
}