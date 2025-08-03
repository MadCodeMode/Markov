using System.Collections.Generic;

namespace Markov.API.Models
{
    public class BacktestResultDto
    {
        public decimal InitialCapital { get; set; }
        public decimal FinalCapital { get; set; }
        public decimal RealizedPnl { get; set; }
        public int WinCount { get; set; }
        public int LossCount { get; set; }
        public int HoldCount { get; set; }
        public double WinRate { get; set; }
        public decimal FinalHeldAssetsValue { get; set; }
        public List<TradeDto> Trades { get; set; } = new List<TradeDto>();
        public List<ChartSeriesDto> Charts { get; set; } = new List<ChartSeriesDto>();
    }
}