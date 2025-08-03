using System;

namespace Markov.API.Models
{
    public class StartLiveSessionRequest
    {
        public Guid StrategyId { get; set; }
        public string Symbol { get; set; }
        public string TimeFrame { get; set; }
    }
}