using Markov.Services.Models;

public class BacktestParameters
{
    public int ConsecutiveMovements { get; set; }
    public decimal StartingCapital { get; set; }
    public TradeSizeMode TradeSizeMode { get; set; }
    public decimal TradeSizeFixedAmount { get; set; }
    public decimal TradeSizePercentage { get; set; }
    public decimal TradeFeePercentage { get; set; }
    public decimal StopLossPercentage { get; set; }
    public decimal ReinvestmentPercentage { get; set; }
    public decimal HoldAccountAnnualYield { get; set; }
    public decimal TakeProfitPercentage { get; set; }

}