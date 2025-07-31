namespace Markov.Services.Models;

public class Candle
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Movement Movement { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public int TradeCount { get; set; }
    public decimal Volume { get; set; }
}
