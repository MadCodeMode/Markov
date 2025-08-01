using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Services;

public class ReversalCalculator : IReversalCalculator
{
    public ReversalProbability CalculateReversalProbability(Asset asset, int consecutiveMovements)
    {
        var upCount = 0;
        var upReversalCount = 0;
        var downCount = 0;
        var downReversalCount = 0;
        var upReversalDates = new List<DateTime>();
        var downReversalDates = new List<DateTime>();

        if (consecutiveMovements <= 0)
        {
            return new ReversalProbability
            {
                UpReversalPercentage = 0,
                DownReversalPercentage = 0,
                UpReversalDates = upReversalDates,
                DownReversalDates = downReversalDates
            };
        }

        var data = asset.HistoricalData;
        var count = data.Count;
        var i = 0;

        while (i <= count - consecutiveMovements - 1)
        {
            var firstMovement = data[i].Movement;
            var sequenceFound = true;

            for (var j = 1; j < consecutiveMovements; j++)
            {
                if (data[i + j].Movement != firstMovement)
                {
                    sequenceFound = false;
                    i += j;
                    break;
                }
            }

            if (sequenceFound)
            {
                if (firstMovement == Movement.Up)
                {
                    upCount++;
                    if (i + consecutiveMovements < count && data[i + consecutiveMovements].Movement == Movement.Down)
                    {
                        upReversalCount++;
                        upReversalDates.Add(data[i + consecutiveMovements].Timestamp);
                    }
                }
                else if (firstMovement == Movement.Down)
                {
                    downCount++;
                    if (i + consecutiveMovements < count && data[i + consecutiveMovements].Movement == Movement.Up)
                    {
                        downReversalCount++;
                        downReversalDates.Add(data[i + consecutiveMovements].Timestamp);
                    }
                }
                i += consecutiveMovements;
            }
        }

        var upReversalPercentage = upCount == 0 ? 0 : (double)upReversalCount / upCount;
        var downReversalPercentage = downCount == 0 ? 0 : (double)downReversalCount / downCount;

        return new ReversalProbability
        {
            UpReversalPercentage = upReversalPercentage,
            DownReversalPercentage = downReversalPercentage,
            UpReversalDates = upReversalDates,
            DownReversalDates = downReversalDates
        };
    }
}
