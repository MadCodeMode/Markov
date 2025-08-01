using Markov.Services.Models;

public class BacktestResult
{
    public ReversalProbability ReversalAnalysis { get; set; }
    public decimal StartingCapital { get; set; }
    public decimal FinalTradingCapital { get; set; }
    public decimal FinalHoldAccountBalance { get; set; }
    public decimal RealizedPNL { get; set; }
    public List<TradeRecord> TradeHistory { get; set; }

    // Total PNL is the sum of all changes across all accounts
    public decimal TotalPNL => (FinalTradingCapital + FinalHoldAccountBalance + RealizedPNL) - StartingCapital;
}