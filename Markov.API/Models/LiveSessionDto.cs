using System;
using System.Collections.Generic;

namespace Markov.API.Models
{
    public class LiveSessionDto
    {
        public Guid SessionId { get; set; }
        public Guid StrategyId { get; set; }
        public string StrategyName { get; set; }
        public string Symbol { get; set; }
        public string Status { get; set; } // e.g., Running, Stopped
        public DateTime StartTime { get; set; }
        public List<TradeDto> Trades { get; set; } = new List<TradeDto>();
        public decimal RealizedPnl { get; set; }
    }
}