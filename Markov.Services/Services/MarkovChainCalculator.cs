using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Services;

public class MarkovChainCalculator : IMarkovChainCalculator
{
    public double CalculateNextMovementProbability(Asset asset, Movement[] pattern, Movement target = Movement.Up)
{
    if (asset == null || asset.HistoricalData == null || pattern == null || pattern.Length == 0)
        throw new ArgumentException("Invalid input parameters.");

    if (asset.HistoricalData.Count < pattern.Length + 1)
        return 0.5;

    int patternOccurrences = 0;
    int patternFollowedByTarget = 0;

    for (int i = 0; i <= asset.HistoricalData.Count - pattern.Length - 1; i++)
    {
        bool isMatch = true;
        for (int j = 0; j < pattern.Length; j++)
        {
            if (asset.HistoricalData[i + j].Movement != pattern[j])
            {
                isMatch = false;
                break;
            }
        }

        if (isMatch)
        {
            patternOccurrences++;
            if (asset.HistoricalData[i + pattern.Length].Movement == target)
                patternFollowedByTarget++;
        }
    }

    return patternOccurrences == 0
        ? 0.5
        : (double)patternFollowedByTarget / patternOccurrences;
}
}
