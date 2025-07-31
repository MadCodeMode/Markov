using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public interface IReversalCalculator
{
    ReversalProbability CalculateReversalProbability(Asset asset, int consecutiveMovements);
}
