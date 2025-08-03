namespace Markov.Services.Interfaces;

public interface IBacktestingEngine
{
    Task<BacktestResult> RunAsync(IStrategy strategy, BacktestParameters parameters);
}