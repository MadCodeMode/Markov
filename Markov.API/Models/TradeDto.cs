using System;

namespace Markov.API.Models
{
    public class TradeDto
    {
        public string Symbol { get; set; }
        public string Side { get; set; }
        public decimal Quantity { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal? ExitPrice { get; set; }
        public decimal Pnl { get; set; }
        public DateTime EntryTimestamp { get; set; }
        public DateTime? ExitTimestamp { get; set; }
        public string Outcome { get; set; }
    }
}