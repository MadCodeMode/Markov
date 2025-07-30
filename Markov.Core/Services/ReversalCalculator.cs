using Markov.Core.Interfaces;
using Markov.Core.Models;

namespace Markov.Core.Services;

public class ReversalCalculator : IReversalCalculator
{
    public double CalculateReversalProbability(Asset asset, int consecutiveMovements)
    {
        // This is the core logic for the reversal probability calculation.
        // We need to find all occurrences of n consecutive 'Up' or 'Down' movements
        // and then count how many times a reversal follows.

        var totalOccurrences = 0;
        var totalReversals = 0;

        if (consecutiveMovements > 0)
        {
            for (var i = 0; i < asset.HistoricalData.Count - consecutiveMovements; i++)
            {
                var isUpSequence = true;
                var isDownSequence = true;

                for (var j = 0; j < consecutiveMovements; j++)
                {
                    if (asset.HistoricalData[i + j].Movement != Movement.Up)
                    {
                        isUpSequence = false;
                    }
                    if (asset.HistoricalData[i + j].Movement != Movement.Down)
                    {
                        isDownSequence = false;
                    }
                }

                if (isUpSequence)
                {
                    totalOccurrences++;
                    if (asset.HistoricalData[i + consecutiveMovements].Movement == Movement.Down)
                    {
                        totalReversals++;
                    }
                }

                if (isDownSequence)
                {
                    totalOccurrences++;
                    if (asset.HistoricalData[i + consecutiveMovements].Movement == Movement.Up)
                    {
                        totalReversals++;
                    }
                }
            }
        }

        if (totalOccurrences == 0)
        {
            return 0.5; // If the pattern is not found, we assume a 50% probability for reversal.
        }

        return (double)totalReversals / totalOccurrences;
    }
}
