namespace Markov.Services.Models;

public enum Movement
{
    Up,
    Down
}

public enum AssetType
{
    Crypto,
    Stock,
    Gold,
    Bonds,
    Realestate
}

// --- Enums for Configuration ---
public enum TradeSizeMode
{
    FixedAmount,
    PercentageOfCapital
}

public enum TradeOutcome
{
    TakeProfit,
    StopLoss,
    MovedToHold,
    Continuation // No action taken
}