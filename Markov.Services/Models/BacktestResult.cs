using Markov.Services.Models;

public class BacktestResult
{
public decimal StartingCapital { get; set; }
    public decimal FinalTradingCapital { get; set; }
    public decimal FinalHoldAccountAssetQuantity { get; set; } // Tracks quantity of asset
    public decimal FinalHoldAccountValue { get; set; } // Final USD value of held asset
    public decimal RealizedPNL { get; set; }
    public int WinCount { get; set; }
    public int LossCount { get; set; }
    public int HoldMoveCount { get; set; }
    public List<TradeRecord> TradeHistory { get; set; }
}