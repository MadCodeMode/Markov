using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public interface IReversalCalculator
{
    double CalculateReversalProbability(Asset asset, int consecutiveMovements);
}
