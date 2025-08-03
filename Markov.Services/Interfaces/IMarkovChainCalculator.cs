using Markov.Services.Enums;
using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public interface IMarkovChainCalculator
{
    double CalculateNextMovementProbability(Asset asset, Movement[] pattern, Movement target = Movement.Up);
}
