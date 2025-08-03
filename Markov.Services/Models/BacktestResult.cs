using Markov.Services.Models;

public class BacktestResult
{
    public decimal InitialCapital { get; set; }
    public decimal FinalCapital { get; set; }
    public decimal RealizedPnl { get; set; }
    public int WinCount { get; set; }
    public int LossCount { get; set; }
    public int HoldCount { get; set; }
    public List<Trade> Trades { get; set; } = new List<Trade>();
    public Dictionary<string, decimal> HeldAssets { get; set; } = new Dictionary<string, decimal>();
    public decimal FinalHeldAssetsValue { get; set; }
}