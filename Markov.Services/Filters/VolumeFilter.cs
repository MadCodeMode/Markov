using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Strategies.Filters;

    public class VolumeFilter : IStrategyFilter
    {
        private readonly BacktestParameters _params;
        private readonly List<Candle> _candles;
        private readonly IIndicatorProvider _indicators;

        public VolumeFilter(BacktestParameters parameters, List<Candle> candles, IIndicatorProvider indicators)
        {
            _params = parameters;
            _candles = candles;
            _indicators = indicators;
        }

        public bool IsValid(int index, out string reason)
        {
            reason = string.Empty;
            if (index <= 0 || index >= _candles.Count) return false;

            decimal volume = _candles[index].Volume;
            decimal[] volumeMa = _indicators.GetVolumeSma(_params.VolumeMAPeriod);
            if (volumeMa.Length <= index) return false;

            decimal vma = volumeMa[index];
            bool valid = volume >= vma * _params.MinVolumeMultiplier;

            if (!valid) return false;

            reason = $"Volume {volume:F2} vs VMA({_params.VolumeMAPeriod}) {vma:F2}";
            return true;
        }
    }
