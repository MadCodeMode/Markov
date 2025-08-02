using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Strategies;

public class TrendFilter : IStrategyFilter
{
    private readonly BacktestParameters _params;
    private readonly List<Candle> _candles;
    private readonly IIndicatorProvider _indicators;

    public TrendFilter(BacktestParameters parameters, List<Candle> candles, IIndicatorProvider indicators)
    {
        _params = parameters;
        _candles = candles;
        _indicators = indicators;
    }

    public bool IsValid(int index, out string reason)
    {
        reason = string.Empty;
        if (index <= 0 || index >= _candles.Count) return false;

        decimal price = _candles[index].Close;
        decimal ma = _indicators.GetSma(_params.LongTermMAPeriod)[index];

        bool valid = (_candles[index - 1].Movement == Movement.Down && price >= ma) ||
                     (_candles[index - 1].Movement == Movement.Up && price <= ma);

        if (!valid) return false;

        reason = $"Price {price:F2} vs MA({_params.LongTermMAPeriod}) {ma:F2}";
        return true;
    }
}
