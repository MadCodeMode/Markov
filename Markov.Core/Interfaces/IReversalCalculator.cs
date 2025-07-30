using Markov.Core.Models;

namespace Markov.Core.Interfaces;

public interface IReversalCalculator
{
    double CalculateReversalProbability(Asset asset, int consecutiveMovements);
}
