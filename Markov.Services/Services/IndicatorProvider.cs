using Markov.Services.Helpers;
using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public class IndicatorProvider : IIndicatorProvider
    {
        private readonly List<Candle> _candles;

        private readonly Dictionary<int, decimal[]> _smaCache = new();
        private readonly Dictionary<int, decimal[]> _volumeSmaCache = new();
        private readonly Dictionary<int, decimal[]> _rsiCache = new();
        private readonly Dictionary<int, decimal[]> _atrCache = new();

        public IndicatorProvider(List<Candle> candles)
        {
            _candles = candles;
        }

        public decimal[] GetSma(int period)
        {
            if (!_smaCache.TryGetValue(period, out var result))
            {
                result = IndicatorCalculatorHelpers.CalculateSma(_candles, period);
                _smaCache[period] = result;
            }
            return result;
        }

        public decimal[] GetVolumeSma(int period)
        {
            if (!_volumeSmaCache.TryGetValue(period, out var result))
            {
                result = IndicatorCalculatorHelpers.CalculateVolumeSma(_candles, period);
                _volumeSmaCache[period] = result;
            }
            return result;
        }

        public decimal[] GetRsi(int period)
        {
            if (!_rsiCache.TryGetValue(period, out var result))
            {
                result = IndicatorCalculatorHelpers.CalculateRsi(_candles, period);
                _rsiCache[period] = result;
            }
            return result;
        }

        public decimal[] GetAtr(int period)
        {
            if (!_atrCache.TryGetValue(period, out var result))
            {
                result = IndicatorCalculatorHelpers.CalculateAtr(_candles, period);
                _atrCache[period] = result;
            }
            return result;
        }
    }