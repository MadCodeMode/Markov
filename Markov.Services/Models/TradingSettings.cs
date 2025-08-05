namespace Markov.Services.Models
{
    public class TradingSettings
    {
        public Enums.TradeSizeMode SizeMode { get; set; }
        public decimal Size { get; set; } // Represents a fixed amount or a percentage
        public int TradingLoopIntervalSeconds { get; set; } = 60;
    }
}