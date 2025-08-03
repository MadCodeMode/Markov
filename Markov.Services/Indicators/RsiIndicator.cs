using Markov.Services.Interfaces;
using Markov.Services.Models;

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
            var rsiValues = new List<decimal>();
            var candleList = candles.ToList();
            if (candleList.Count < _period)
            {
                return rsiValues;
            }

            var gains = new List<decimal>();
            var losses = new List<decimal>();

            for (int i = 1; i < candleList.Count; i++)
            {
                var change = candleList[i].Close - candleList[i - 1].Close;
                if (change > 0)
                {
                    gains.Add(change);
                    losses.Add(0);
                }
                else
                {
                    gains.Add(0);
                    losses.Add(-change);
                }
            }

            decimal avgGain = gains.Take(_period).Average();
            decimal avgLoss = losses.Take(_period).Average();

            for (int i = _period; i < gains.Count; i++)
            {
                avgGain = (avgGain * (_period - 1) + gains[i]) / _period;
                avgLoss = (avgLoss * (_period - 1) + losses[i]) / _period;
                var rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                var rsi = 100 - (100 / (1 + rs));
                rsiValues.Add(rsi);
            }
            return rsiValues;
        }
    }
}