using Markov.Services.Models;
using Markov.Services.Helpers;

namespace Markov.Services.Strategies;

public abstract class StrategyBase : ITradingStrategy
{
    // Properties accessible by all child strategies
    protected List<Candle> Candles { get; private set; } = new();
    protected BacktestParameters Parameters { get; }

    // Pre-calculated indicators
    protected decimal[] LongTermMA { get; private set; } = Array.Empty<decimal>();
    protected decimal[] VolumeMA { get; private set; } = Array.Empty<decimal>();
    protected decimal[] Rsi { get; private set; } = Array.Empty<decimal>();
    protected decimal[] Atr { get; private set; } = Array.Empty<decimal>();

    protected StrategyBase(BacktestParameters parameters)
    {
        Parameters = parameters;
    }

    public virtual void Initialize(List<Candle> history)
    {
        Candles = history;

        // Pre-calculate all indicators once for efficiency
        // NOTE: These would ideally be in a separate, testable IndicatorService class.
        // For now, we'll assume the static methods from your original file are moved to a helper.
        LongTermMA = IndicatorCalculatorHelpers.CalculateSma(Candles, Parameters.LongTermMAPeriod);
        VolumeMA = IndicatorCalculatorHelpers.CalculateVolumeSma(Candles, Parameters.VolumeMAPeriod);
        Rsi = IndicatorCalculatorHelpers.CalculateRsi(Candles, Parameters.RsiPeriod);
        Atr = IndicatorCalculatorHelpers.CalculateAtr(Candles, Parameters.AtrPeriod);
    }

    public abstract TradeSignal? GenerateSignal(int currentIndex);
}