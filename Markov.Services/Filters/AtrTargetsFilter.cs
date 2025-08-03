using Markov.Services.Enums;
using Markov.Services.Indicators;
using Markov.Services.Models;

namespace Markov.Services.Filters;

public class AtrTargetsFilter : ISignalFilter
{
    private readonly int _atrPeriod;
    private readonly decimal _takeProfitAtrMultiplier;
    private readonly decimal _stopLossAtrMultiplier;
    private readonly AtrIndicator _atrIndicator;

    public AtrTargetsFilter(int atrPeriod = 14, decimal takeProfitAtrMultiplier = 2.0m, decimal stopLossAtrMultiplier = 1.5m)
    {
        _atrPeriod = atrPeriod;
        _takeProfitAtrMultiplier = takeProfitAtrMultiplier;
        _stopLossAtrMultiplier = stopLossAtrMultiplier;
        _atrIndicator = new AtrIndicator(_atrPeriod);
    }

    public string Name => $"AtrTargetsFilter({_atrPeriod}, {_takeProfitAtrMultiplier}, {_stopLossAtrMultiplier})";

    public IEnumerable<Signal> Apply(IEnumerable<Signal> signals, IDictionary<string, IEnumerable<Candle>> data)
    {
        foreach (var signal in signals)
        {
            if (!data.ContainsKey(signal.Symbol)) continue;

            var candles = data[signal.Symbol].ToList();
            var atrValues = _atrIndicator.Calculate(candles).ToList();
            var signalCandleIndex = candles.FindIndex(c => c.Timestamp == signal.Timestamp);

            if (signalCandleIndex == -1 || signalCandleIndex >= atrValues.Count) continue;

            var atrValue = atrValues[signalCandleIndex];
            if (atrValue <= 0) continue;

            if (signal.Type == SignalType.Buy)
            {
                signal.TakeProfit = signal.Price + (atrValue * _takeProfitAtrMultiplier);
                signal.StopLoss = signal.Price - (atrValue * _stopLossAtrMultiplier);
            }
            else if (signal.Type == SignalType.Sell)
            {
                signal.TakeProfit = signal.Price - (atrValue * _takeProfitAtrMultiplier);
                signal.StopLoss = signal.Price + (atrValue * _stopLossAtrMultiplier);
            }
        }
        return signals;
    }
}