using Markov.Services.Models;

namespace Markov.Services.Helpers;

public static class IndicatorCalculatorHelpers{
    // --- INDICATOR CALCULATION HELPERS ---
public static decimal[] CalculateSma(List<Candle> candles, int period)
{
    // Add guard clause to prevent division by zero
    if (period <= 0) return new decimal[candles.Count];

    var smaValues = new decimal[candles.Count];
    for (int i = period - 1; i < candles.Count; i++)
    {
        decimal sum = 0;
        for (int j = 0; j < period; j++)
        {
            sum += candles[i - j].Close;
        }
        smaValues[i] = sum / period;
    }
    return smaValues;
}

public static decimal[] CalculateVolumeSma(List<Candle> candles, int period)
{
    // Add guard clause to prevent division by zero
    if (period <= 0) return new decimal[candles.Count];

    var volumeSmaValues = new decimal[candles.Count];
    for (int i = period - 1; i < candles.Count; i++)
    {
        decimal sum = 0;
        for (int j = 0; j < period; j++)
        {
            sum += candles[i - j].Volume;
        }
        volumeSmaValues[i] = sum / period;
    }
    return volumeSmaValues;
}

public static decimal[] CalculateAtr(List<Candle> candles, int period)
{
    // Add guard clause for period and data length
    if (period <= 0 || candles.Count < period) return new decimal[candles.Count];

    var atrValues = new decimal[candles.Count];
    decimal[] trValues = new decimal[candles.Count];
    
    // First TR is just High-Low, but we need a previous close for the rest
    trValues[0] = candles[0].High - candles[0].Low;
    for (int i = 1; i < candles.Count; i++)
    {
        decimal tr1 = candles[i].High - candles[i].Low;
        decimal tr2 = Math.Abs(candles[i].High - candles[i - 1].Close);
        decimal tr3 = Math.Abs(candles[i].Low - candles[i - 1].Close);
        trValues[i] = Math.Max(tr1, Math.Max(tr2, tr3));
    }
    
    decimal firstAtr = 0;
    for (int i = 0; i < period; i++) firstAtr += trValues[i];
    atrValues[period - 1] = firstAtr / period;
    
    for (int i = period; i < candles.Count; i++)
    {
        atrValues[i] = (atrValues[i - 1] * (period - 1) + trValues[i]) / period;
    }

    return atrValues;
}

public static decimal[] CalculateRsi(List<Candle> candles, int period)
{
    // Add guard clause for period and data length
    if (period <= 0 || candles.Count <= period) return new decimal[candles.Count];

    var rsiValues = new decimal[candles.Count];
    decimal avgGain = 0;
    decimal avgLoss = 0;

    for (int i = 1; i <= period; i++)
    {
        decimal change = candles[i].Close - candles[i - 1].Close;
        if (change > 0) avgGain += change;
        else avgLoss -= change;
    }
    
    avgGain /= period;
    avgLoss /= period;
    
    if (avgLoss == 0) rsiValues[period] = 100;
    else rsiValues[period] = 100 - (100 / (1 + (avgGain / avgLoss)));
    
    for (int i = period + 1; i < candles.Count; i++)
    {
        decimal change = candles[i].Close - candles[i - 1].Close;
        decimal gain = change > 0 ? change : 0;
        decimal loss = change < 0 ? -change : 0;

        avgGain = (avgGain * (period - 1) + gain) / period;
        avgLoss = (avgLoss * (period - 1) + loss) / period;
        
        if (avgLoss == 0) rsiValues[i] = 100;
        else rsiValues[i] = 100 - (100 / (1 + (avgGain / avgLoss)));
    }

    return rsiValues;
}
}