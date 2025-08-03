using Markov.Services.Enums;
using Markov.Services.Interfaces;

public class BacktestParameters
{
      public string Symbol { get; set; } = string.Empty;
        public TimeFrame TimeFrame { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal InitialCapital { get; set; }
        public IExchange Exchange { get; set; } = null!;
        public TradeSizeMode TradeSizeMode { get; set; } = TradeSizeMode.PercentageOfCapital;
        public decimal TradeSizeValue { get; set; } = 0.1m; // Default to 10%
        public decimal CommissionPercentage { get; set; } = 0m; // Default to 0%
        public decimal SlippagePercentage { get; set; } = 0m; // Default to 0%
}