using Markov.Services.Execution;
using Markov.Services.Models;
using Markov.Services.Strategies;

namespace Markov.Services.Core;

public class TradingEngine
{
    private readonly ITradingStrategy _strategy;
    private readonly IExecutionHandler _executionHandler;
    private readonly List<Candle> _candles;

    public TradingEngine(ITradingStrategy strategy, IExecutionHandler executionHandler, List<Candle> candles)
    {
        _strategy = strategy;
        _executionHandler = executionHandler;
        _candles = candles;
    }

    public BacktestResult Run()
    {
        // 1. Initialize components
        _strategy.Initialize(_candles);

        // 2. Main Event Loop
        // Loop stops before the last candle to allow execution on the next bar.
        for (int i = 0; i < _candles.Count - 1; i++)
        {
            // Ask the strategy to generate a signal based on data up to index 'i'
            var signal = _strategy.GenerateSignal(i);

            if (signal != null)
            {
                // Pass the signal and the current candle to the execution handler.
                // The handler will execute the trade on the *next* candle's open.
                _executionHandler.ProcessSignal(signal, _candles[i], i);
            }
        }

        // 3. Finalize and get results
        return _executionHandler.GetResult();
    }
}