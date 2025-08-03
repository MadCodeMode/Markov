using Markov.Services.Enums;
using Markov.Services.Indicators;
using Markov.Services.Models;

namespace Markov.Services.Strategies
{
    public class SimpleMacdStrategy : BaseStrategy
    {
        public override string Name => "Simple MACD Strategy";

        public SimpleMacdStrategy()
        {
            AddIndicator(new SmaIndicator(12));
            AddIndicator(new SmaIndicator(26));
        }

        public override IEnumerable<Signal> GenerateSignals(IDictionary<string, IEnumerable<Candle>> data)
        {
            var signals = new List<Signal>();
            var fastSma = Indicators["SMA(12)"];
            var slowSma = Indicators["SMA(26)"];

            foreach (var symbol in data.Keys)
            {
                var candles = data[symbol].ToList();
                var fastSmaValues = fastSma.Calculate(candles).ToList();
                var slowSmaValues = slowSma.Calculate(candles).ToList();

                for (int i = 1; i < candles.Count; i++)
                {
                    if (fastSmaValues[i - 1] < slowSmaValues[i - 1] && fastSmaValues[i] > slowSmaValues[i])
                    {
                        signals.Add(new Signal { Symbol = symbol, Type = SignalType.Buy, Timestamp = candles[i].Timestamp });
                    }
                    else if (fastSmaValues[i - 1] > slowSmaValues[i - 1] && fastSmaValues[i] < slowSmaValues[i])
                    {
                        signals.Add(new Signal { Symbol = symbol, Type = SignalType.Sell, Timestamp = candles[i].Timestamp });
                    }
                }
            }
            return signals;
        }
    }
}