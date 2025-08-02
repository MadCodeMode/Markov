using Markov.Services.Models;

namespace Markov.Services.Strategies;

public class MeanReversionStrategy : StrategyBase
{
    public MeanReversionStrategy(BacktestParameters parameters) : base(parameters) { }

    public override TradeSignal? GenerateSignal(int currentIndex)
    {
        int requiredDataLength = Parameters.ConsecutiveMovements + 1;
        if (currentIndex < Parameters.ConsecutiveMovements) return null;

        // --- 1. Identify the Core Signal: N consecutive moves ---
        var sequenceStartIndex = currentIndex - Parameters.ConsecutiveMovements;
        var firstMovement = Candles[sequenceStartIndex].Movement;
        
        bool isSequence = true;
        for (int j = 1; j < Parameters.ConsecutiveMovements; j++)
        {
            if (Candles[sequenceStartIndex + j].Movement != firstMovement)
            {
                isSequence = false;
                break;
            }
        }

        if (!isSequence) return null;

        // --- 2. Apply Filters ---
        var signalCandle = Candles[currentIndex - 1]; // The last candle of the sequence
        var currentPrice = signalCandle.Close;
        var conditions = new Dictionary<string, string>();

        // Trend Filter
        if (Parameters.EnableTrendFilter)
        {
            decimal maValue = LongTermMA[currentIndex - 1];
            if ((firstMovement == Movement.Down && currentPrice < maValue) ||
                (firstMovement == Movement.Up && currentPrice > maValue))
            {
                return null; // Filtered out
            }
            conditions.Add("Trend Filter", $"Price {currentPrice} vs MA({Parameters.LongTermMAPeriod}) {maValue:F2}");
        }

        // RSI Filter
        if (Parameters.EnableRsiFilter)
        {
            decimal rsiValue = Rsi[currentIndex - 1];
            if ((firstMovement == Movement.Down && rsiValue > Parameters.RsiOversoldThreshold) ||
                (firstMovement == Movement.Up && rsiValue < Parameters.RsiOverboughtThreshold))
            {
                return null; // Filtered out
            }
            conditions.Add("RSI Filter", $"RSI({Parameters.RsiPeriod}) is {rsiValue:F2}");
        }

        // Volume Filter
        if (Parameters.EnableVolumeFilter)
        {
            decimal volValue = signalCandle.Volume;
            decimal volMaValue = VolumeMA[currentIndex - 1];
            if (volValue < volMaValue * Parameters.MinVolumeMultiplier)
            {
                return null; // Filtered out
            }
            conditions.Add("Volume Filter", $"Volume {volValue:F2} vs VMA({Parameters.VolumeMAPeriod}) {volMaValue:F2}");
        }

        // --- 3. Generate Signal ---
        SignalType signalType = firstMovement == Movement.Down ? SignalType.Long : SignalType.Short;
        conditions.Add("Core Signal", $"{Parameters.ConsecutiveMovements} consecutive {firstMovement} bars.");

        return new TradeSignal(signalType, conditions);
    }
}