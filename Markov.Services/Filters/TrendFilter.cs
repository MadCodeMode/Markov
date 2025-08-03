using Markov.Services.Enums;
using Markov.Services.Indicators;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using System.Collections.Generic;
using System.Linq;

namespace Markov.Services.Filters
{
    public class TrendFilter : ISignalFilter
    {
        private readonly IIndicator _smaIndicator;

        public TrendFilter(int longTermMAPeriod = 200)
            : this(new SmaIndicator(longTermMAPeriod))
        {
            Name = $"TrendFilter({longTermMAPeriod})";
        }
        
        public TrendFilter(IIndicator smaIndicator)
        {
            _smaIndicator = smaIndicator;
            Name = "TrendFilter";
        }

        public string Name { get; }

        public IEnumerable<Signal> Apply(IEnumerable<Signal> signals, IDictionary<string, IEnumerable<Candle>> data)
        {
            var filteredSignals = new List<Signal>();

            foreach (var signal in signals)
            {
                if (!data.ContainsKey(signal.Symbol)) continue;

                var candles = data[signal.Symbol].ToList();
                var smaValues = _smaIndicator.Calculate(candles).ToList();
                var signalCandleIndex = candles.FindIndex(c => c.Timestamp == signal.Timestamp);

                if (signalCandleIndex == -1 || signalCandleIndex >= smaValues.Count || smaValues[signalCandleIndex] == 0) continue;

                decimal currentPrice = candles[signalCandleIndex].Close;
                decimal maValue = smaValues[signalCandleIndex];
                
                if (signal.Type == SignalType.Buy && currentPrice < maValue) continue;
                if (signal.Type == SignalType.Sell && currentPrice > maValue) continue;

                filteredSignals.Add(signal);
            }

            return filteredSignals;
        }
    }
}