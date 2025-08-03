using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Indicators;

public class AtrIndicator : IIndicator
{
    private readonly int _period;

    public AtrIndicator(int period = 14)
    {
        _period = period;
    }

    public string Name => $"ATR({_period})";

    public IEnumerable<decimal> Calculate(IEnumerable<Candle> candles)
    {
        var candleList = candles.ToList();
        if (candleList.Count < _period)
        {
            return Enumerable.Repeat(0m, candleList.Count);
        }

        var trValues = new decimal[candleList.Count];
        var atrValues = new decimal[candleList.Count];

        // Calculate True Range (TR) for each candle
        trValues[0] = candleList[0].High - candleList[0].Low;
        for (int i = 1; i < candleList.Count; i++)
        {
            decimal highLow = candleList[i].High - candleList[i].Low;
            decimal highPrevClose = Math.Abs(candleList[i].High - candleList[i - 1].Close);
            decimal lowPrevClose = Math.Abs(candleList[i].Low - candleList[i - 1].Close);
            trValues[i] = Math.Max(highLow, Math.Max(highPrevClose, lowPrevClose));
        }

        // Calculate the first ATR value as a simple moving average of the first 'period' TR values
        decimal firstAtr = 0;
        for (int i = 0; i < _period; i++)
        {
            firstAtr += trValues[i];
        }
        atrValues[_period - 1] = firstAtr / _period;

        // Calculate subsequent ATR values using the smoothing formula
        for (int i = _period; i < candleList.Count; i++)
        {
            atrValues[i] = (atrValues[i - 1] * (_period - 1) + trValues[i]) / _period;
        }

        return atrValues;
    }
}