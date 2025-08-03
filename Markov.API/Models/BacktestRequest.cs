using System;

namespace Markov.API.Models
{
    public class BacktestRequest
    {
        public Guid StrategyId { get; set; }
        public string Symbol { get; set; }
        public string TimeFrame { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal InitialCapital { get; set; }
    }
}