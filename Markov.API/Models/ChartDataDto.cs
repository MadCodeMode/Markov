using System;

namespace Markov.API.Models
{
    public class ChartDataDto
    {
        public DateTime Timestamp { get; set; }
        public decimal Value { get; set; }
    }
}