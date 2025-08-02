using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public interface IReversalCalculator
{
    public ReversalProbability CalculateReversalProbability(Asset asset, BacktestParameters parameters);
}
