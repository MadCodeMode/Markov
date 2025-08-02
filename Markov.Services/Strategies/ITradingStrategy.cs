using Markov.Services.Models;

namespace Markov.Services.Strategies;

public interface ITradingStrategy
{
    /// <summary>
    /// Initializes the strategy with historical data and pre-calculates indicators.
    /// </summary>
    void Initialize(List<Candle> history);

    /// <summary>
    /// Checks the conditions at the current data point and generates a trade signal if necessary.
    /// </summary>
    /// <param name="currentIndex">The index of the current candle in the historical data.</param>
    /// <returns>A TradeSignal object or null if no action is warranted.</returns>
    TradeSignal? GenerateSignal(int currentIndex);
}