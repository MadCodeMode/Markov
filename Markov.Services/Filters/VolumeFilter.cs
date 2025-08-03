using Markov.Services.Indicators;
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Filters;

public class VolumeFilter : ISignalFilter
{
    private readonly IIndicator _volumeMaIndicator;
    private readonly decimal _minVolumeMultiplier;

    public VolumeFilter(int volumeMAPeriod = 20, decimal minVolumeMultiplier = 1.5m)
        : this(new SmaIndicator(volumeMAPeriod))
    {
        _minVolumeMultiplier = minVolumeMultiplier;
        Name = $"VolumeFilter({volumeMAPeriod}, {minVolumeMultiplier})";
    }

    public VolumeFilter(IIndicator volumeMaIndicator)
    {
        _volumeMaIndicator = volumeMaIndicator;
        _minVolumeMultiplier = 1.5m; // Default for test simplicity
        Name = "VolumeFilter";
    }

    public string Name { get; }

    public IEnumerable<Signal> Apply(IEnumerable<Signal> signals, IDictionary<string, IEnumerable<Candle>> data)
    {
        var filteredSignals = new List<Signal>();

        foreach (var signal in signals)
        {
            if (!data.ContainsKey(signal.Symbol)) continue;

            var candles = data[signal.Symbol].ToList();
            var volumeMaValues = _volumeMaIndicator.Calculate(candles.Select(c => new Candle { Close = c.Volume })).ToList();
            var signalCandleIndex = candles.FindIndex(c => c.Timestamp == signal.Timestamp);

            if (signalCandleIndex == -1 || signalCandleIndex >= volumeMaValues.Count) continue;

            var volumeValue = candles[signalCandleIndex].Volume;
            var maVolumeValue = volumeMaValues[signalCandleIndex];
            
            if (volumeValue < maVolumeValue * _minVolumeMultiplier) continue;

            filteredSignals.Add(signal);
        }

        return filteredSignals;
    }
}