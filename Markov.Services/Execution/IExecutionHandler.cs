using Markov.Services.Models;

namespace Markov.Services.Execution;

public interface IExecutionHandler
{
    void ProcessSignal(TradeSignal signal, Candle currentCandle);
    BacktestResult GetResult();
}