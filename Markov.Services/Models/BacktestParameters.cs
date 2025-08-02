using Markov.Services.Models;

public class BacktestParameters
{
    public decimal StartingCapital { get; set; }
    public int ConsecutiveMovements { get; set; }
    public decimal TradeFeePercentage { get; set; }
    public decimal ReinvestmentPercentage { get; set; }

    // Trade Sizing
    public TradeSizeMode TradeSizeMode { get; set; }
    public decimal TradeSizeFixedAmount { get; set; }
    public decimal TradeSizePercentage { get; set; }

    // Hold Account (No longer used, but kept for context if needed)
    public decimal HoldAccountAnnualYield { get; set; }


    // Dynamic Risk Targets
    public bool EnableAtrTargets { get; set; } = false;
    public int AtrPeriod { get; set; } = 14;
    public decimal TakeProfitAtrMultiplier { get; set; } = 2.0m;
    public decimal StopLossAtrMultiplier { get; set; } = 1.5m;

    // Original Percentage-Based Risk Targets
    public decimal StopLossPercentage { get; set; }
    public decimal TakeProfitPercentage { get; set; }

    // Trend Filter
    public bool EnableTrendFilter { get; set; } = false;
    public int LongTermMAPeriod { get; set; } = 50;

    // RSI Filter
    public bool EnableRsiFilter { get; set; } = false;
    public int RsiPeriod { get; set; } = 14;
    public decimal RsiOverboughtThreshold { get; set; } = 70;
    public decimal RsiOversoldThreshold { get; set; } = 30;

    // Volume Filter
    public bool EnableVolumeFilter { get; set; } = false;
    public int VolumeMAPeriod { get; set; } = 20;
    public decimal MinVolumeMultiplier { get; set; } = 1.5m;

}