using Markov.Services.Enums;

namespace Markov.Services.Models;

public class Signal
{
    public string Symbol { get; set; }
    public SignalType Type { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
    public bool UseHoldStrategy { get; set; } = false;
}