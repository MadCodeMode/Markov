using Markov.Services.Enums;

public class TradeRecord
{
    public DateTime Timestamp { get; set; }
    public string Signal { get; set; } // e.g., "Long Entry after 3 Down"
    public TradeOutcome Outcome { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal AmountInvested { get; set; }
    public decimal Pnl { get; set; } // PNL from this specific trade
    public string Notes { get; set; }
}