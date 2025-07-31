using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Services;

public class ReversalCalculator : IReversalCalculator
{
    public ReversalProbability CalculateReversalProbability(Asset asset, int consecutiveMovements)
    {
        var upOccurrences = 0;
        var upReversals = 0;
        var downOccurrences = 0;
        var downReversals = 0;
        var upReversalDates = new List<DateTime>();
        var downReversalDates = new List<DateTime>();

        if (consecutiveMovements > 0)
        {
            for (var i = 0; i < asset.HistoricalData.Count - consecutiveMovements; i++)
            {
                var isUpSequence = true;
                for (var j = 0; j < consecutiveMovements; j++)
                {
                    if (asset.HistoricalData[i + j].Movement != Movement.Up)
                    {
                        isUpSequence = false;
                        break;
                    }
                }

                if (isUpSequence)
                {
                    upOccurrences++;
                    if (asset.HistoricalData[i + consecutiveMovements].Movement == Movement.Down)
                    {
                        upReversals++;
                        upReversalDates.Add(asset.HistoricalData[i + consecutiveMovements].Timestamp);
                    }
                }

                var isDownSequence = true;
                for (var j = 0; j < consecutiveMovements; j++)
                {
                    if (asset.HistoricalData[i + j].Movement != Movement.Down)
                    {
                        isDownSequence = false;
                        break;
                    }
                }

                if (isDownSequence)
                {
                    downOccurrences++;
                    if (asset.HistoricalData[i + consecutiveMovements].Movement == Movement.Up)
                    {
                        downReversals++;
                        downReversalDates.Add(asset.HistoricalData[i + consecutiveMovements].Timestamp);
                    }
                }
            }
        }

        var upReversalPercentage = upOccurrences == 0 ? 0.5 : (double)upReversals / upOccurrences;
        var downReversalPercentage = downOccurrences == 0 ? 0.5 : (double)downReversals / downOccurrences;

        return new ReversalProbability
        {
            UpReversalPercentage = upReversalPercentage,
            DownReversalPercentage = downReversalPercentage,
            UpReversalDates = upReversalDates,
            DownReversalDates = downReversalDates
        };
    }
}
