using Markov.Services.Models;

public class BacktestParameters
{
    public int ConsecutiveMovements { get; set; }
    public decimal StartingCapital { get; set; } = 10000;
    public TradeSizeMode TradeSizeMode { get; set; } = TradeSizeMode.PercentageOfCapital;
    public decimal TradeSizeFixedAmount { get; set; } = 1000; // Used if mode is FixedAmount
    public decimal TradeSizePercentage { get; set; } = 0.02m; // 2% of capital, used if mode is Percentage
    public decimal TradeFeePercentage { get; set; } = 0.001m; // 0.1% fee
    public decimal StopLossPercentage { get; set; } = 0.02m; // 2% stop-loss for shorts
    public decimal ReinvestmentPercentage { get; set; } = 0.5m; // 50%
    public decimal HoldAccountAnnualYield { get; set; } = 0.05m; // 5% APY
}