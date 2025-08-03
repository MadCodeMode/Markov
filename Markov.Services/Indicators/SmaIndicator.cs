using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Indicators
{
    public class SmaIndicator : IIndicator
    {
        private readonly int _period;

        public SmaIndicator(int period)
        {
            _period = period;
        }

        public string Name => $"SMA({_period})";

        public IEnumerable<decimal> Calculate(IEnumerable<Candle> candles)
        {
            var smaValues = new List<decimal>();
            var candleList = candles.ToList();
            for (int i = 0; i < candleList.Count; i++)
            {
                if (i < _period - 1)
                {
                    smaValues.Add(0); 
                }
                else
                {
                    decimal sum = 0;
                    for (int j = 0; j < _period; j++)
                    {
                        sum += candleList[i - j].Close;
                    }
                    smaValues.Add(sum / _period);
                }
            }
            return smaValues;
        }
    }
}