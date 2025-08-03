using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Services;

/// <summary>
/// Calculates the historical probability of a price movement reversing
/// after a specified number of consecutive movements in the same direction.
/// </summary>
public class ReversalCalculator : IReversalCalculator
{
    /// <summary>
    /// Analyzes historical asset data to determine reversal probabilities.
    /// </summary>
    /// <param name="asset">The asset with its historical data.</param>
    /// <param name="parameters">Parameters for the backtest, including the number of consecutive movements to look for.</param>
    /// <returns>A <see cref="ReversalProbability"/> object containing the calculated percentages and underlying data points.</returns>
    public ReversalProbability CalculateReversalProbability(Asset asset, BacktestParameters parameters)
    {
        int consecutiveMovements = parameters.ConsecutiveMovements;
        
        // A sequence of 0 or less makes no sense, return an empty result.
        if (consecutiveMovements <= 0)
        {
            // Assuming ReversalProbability has a parameterless constructor
            // and collections are initialized empty.
            return new ReversalProbability();
        }

        var data = asset.HistoricalData;
        var state = new CalculationState();
        
        // We need at least 'consecutiveMovements' plus one more candle to check for reversal.
        int requiredDataLength = consecutiveMovements + 1;
        int i = 0;

        while (i <= data.Count - requiredDataLength)
        {
            // Try to find and process a sequence at the current position 'i'.
            if (TryProcessSequenceAt(i, data, consecutiveMovements, state))
            {
                // If a sequence was found and processed, skip past it.
                i += consecutiveMovements;
            }
            else
            {
                // If no sequence was found, just move to the next candle.
                i++;
            }
        }

        return state.ToReversalProbability();
    }

    /// <summary>
    /// Checks for a consecutive sequence at a given index and records the outcome if found.
    /// </summary>
    /// <returns>True if a sequence was found, otherwise false.</returns>
    private bool TryProcessSequenceAt(int index, IReadOnlyList<Candle> data, int sequenceLength, CalculationState state)
    {
        var firstMovement = data[index].Movement;

        // Check if the next 'sequenceLength' candles all have the same movement.
        for (var j = 1; j < sequenceLength; j++)
        {
            if (data[index + j].Movement != firstMovement)
            {
                return false; // Sequence broken.
            }
        }

        // A full sequence was found, so record its outcome.
        var outcomeCandle = data[index + sequenceLength];
        RecordOutcome(firstMovement, outcomeCandle, state);
        return true;
    }

    /// <summary>
    /// Records the outcome (reversal or continuation) after a sequence is identified.
    /// </summary>
    private void RecordOutcome(Movement sequenceDirection, Candle outcomeCandle, CalculationState state)
    {
        var dataPoint = new ReversalDataPoint
        {
            Timestamp = outcomeCandle.Timestamp,
            Volume = outcomeCandle.Volume,
            TradeCount = outcomeCandle.TradeCount
        };

        if (sequenceDirection == Movement.Up)
        {
            bool isReversal = outcomeCandle.Movement == Movement.Down;
            state.RecordUpSequence(isReversal, dataPoint);
        }
        else // sequenceDirection == Movement.Down
        {
            bool isReversal = outcomeCandle.Movement == Movement.Up;
            state.RecordDownSequence(isReversal, dataPoint);
        }
    }

    /// <summary>
    /// A private class to encapsulate the state of the calculation.
    /// This avoids polluting the main method with numerous local variables.
    /// </summary>
    private class CalculationState
    {
        private int _upCount;
        private int _upReversalCount;
        private int _downCount;
        private int _downReversalCount;
        
        private readonly List<ReversalDataPoint> _upReversalData = new();
        private readonly List<ReversalDataPoint> _downReversalData = new();
        private readonly List<ReversalDataPoint> _upNonReversalData = new();
        private readonly List<ReversalDataPoint> _downNonReversalData = new();

        public void RecordUpSequence(bool isReversal, ReversalDataPoint dataPoint)
        {
            _upCount++;
            if (isReversal)
            {
                _upReversalCount++;
                _upReversalData.Add(dataPoint);
            }
            else
            {
                _upNonReversalData.Add(dataPoint);
            }
        }

        public void RecordDownSequence(bool isReversal, ReversalDataPoint dataPoint)
        {
            _downCount++;
            if (isReversal)
            {
                _downReversalCount++;
                _downReversalData.Add(dataPoint);
            }
            else
            {
                _downNonReversalData.Add(dataPoint);
            }
        }
        
        public ReversalProbability ToReversalProbability()
        {
            return new ReversalProbability
            {
                UpReversalPercentage = _upCount == 0 ? 0 : (double)_upReversalCount / _upCount,
                DownReversalPercentage = _downCount == 0 ? 0 : (double)_downReversalCount / _downCount,
                UpReversalData = _upReversalData,
                DownReversalData = _downReversalData,
                UpNonReversalData = _upNonReversalData,
                DownNonReversalData = _downNonReversalData
            };
        }
    }
}