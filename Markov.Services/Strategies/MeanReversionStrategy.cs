using Markov.Services.Models;

namespace Markov.Services.Strategies;

public class MeanReversionStrategy : StrategyBase
{
    public MeanReversionStrategy(BacktestParameters parameters) : base(parameters)
    { 

    }
    
    public override void Initialize(List<Candle> history)
    {
        base.Initialize(history);
        RegisterCommonFilters();
    }

    public override TradeSignal? GenerateSignal(int currentIndex)
    {
        if (currentIndex < Parameters.ConsecutiveMovements) return null;

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

        if (!ApplyFilters(currentIndex, out var reasons)) return null;


        SignalType type = firstMovement == Movement.Down ? SignalType.Long : SignalType.Short;
        reasons.Add("Core Signal", $"{Parameters.ConsecutiveMovements} consecutive {firstMovement} bars.");
        return new TradeSignal(type, reasons);
    }
}