using Markov.Services.Interfaces;
using Markov.Services.Models;
using System.Collections.Generic;
using System.Linq;

namespace Markov.Services.Indicators
{
    public class RsiIndicator : IIndicator
    {
        private readonly int _period;

        public RsiIndicator(int period)
        {
            _period = period;
        }

        public string Name => $"RSI({_period})";
        public IEnumerable<decimal> Calculate(IEnumerable<Candle> candles)
        {
            var candleList = candles.ToList();
            if (candleList.Count <= _period)
            {
                return Enumerable.Empty<decimal>();
            }

            var rsiValues = new List<decimal>();
            var gains = new List<decimal>();
            var losses = new List<decimal>();

            for (int i = 1; i < candleList.Count; i++)
            {
                var change = candleList[i].Close - candleList[i - 1].Close;
                gains.Add(change > 0 ? change : 0);
                losses.Add(change < 0 ? -change : 0);
            }

            decimal avgGain = gains.Take(_period).Average();
            decimal avgLoss = losses.Take(_period).Average();

            if (avgLoss == 0)
            {
                rsiValues.Add(100);
            }
            else
            {
                decimal rs = avgGain / avgLoss;
                rsiValues.Add(100 - (100 / (1 + rs)));
            }

            for (int i = _period; i < gains.Count; i++)
            {
                avgGain = (avgGain * (_period - 1) + gains[i]) / _period;
                avgLoss = (avgLoss * (_period - 1) + losses[i]) / _period;

                if (avgLoss == 0)
                {
                    rsiValues.Add(100);
                }
                else
                {
                    decimal rs = avgGain / avgLoss;
                    rsiValues.Add(100 - (100 / (1 + rs)));
                }
            }

            return rsiValues;
        }
    }
}