namespace Markov.Services.Models;

public class TradeResult
{
    public bool IsReversal { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal ProfitLoss { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime ExitTime { get; set; }
}
