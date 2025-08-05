using Markov.Services.Enums;

namespace Markov.Services.Models;

public class TradingSettings
{
    public int TradingLoopIntervalSeconds { get; set; } = 10;
    public TradeSizeMode SizeMode { get; set; } = TradeSizeMode.FixedAmount;
    public decimal Size { get; set; } = 1;
}
