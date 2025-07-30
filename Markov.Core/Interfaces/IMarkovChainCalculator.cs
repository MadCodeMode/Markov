using Markov.Core.Models;

namespace Markov.Core.Interfaces;

public interface IMarkovChainCalculator
{
    double CalculateNextMovementProbability(Asset asset, Movement[] pattern, Movement target = Movement.Up);
}
