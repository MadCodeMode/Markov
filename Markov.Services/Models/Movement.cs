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
    /// <summary>
    /// The trade exited by hitting the take-profit target.
    /// </summary>
    TakeProfit,
    /// <summary>
    /// The trade exited by hitting the stop-loss target.
    /// </summary>
    StopLoss,
    /// <summary>
    /// The trade did not hit a TP or SL and was closed at the end of the day.
    /// </summary>
    CloseOut,
    MovedToHold
}