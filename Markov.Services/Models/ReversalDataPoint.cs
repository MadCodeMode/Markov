namespace Markov.Services.Models;

public class ReversalDataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal Volume { get; set; }
    public int TradeCount { get; set; }
}
