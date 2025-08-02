namespace Markov.Services.Models;

public class TradeSignal
{
    public SignalType Type { get; }
    public Dictionary<string, string> TriggerConditions { get; }

    public TradeSignal(SignalType type, Dictionary<string, string> triggerConditions)
    {
        Type = type;
        TriggerConditions = triggerConditions;
    }
}

public enum SignalType
{
    Long,
    Short,
    ClosePosition // For strategies that need to exit a position
}