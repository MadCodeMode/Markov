using Markov.Services.Enums;

namespace Markov.Services.Models;

public class Trade
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal AmountInvested { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal Pnl { get; set; }
    public DateTime EntryTimestamp { get; set; }
    public DateTime? ExitTimestamp { get; set; }
    public TradeOutcome Outcome { get; set; }

    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
}
