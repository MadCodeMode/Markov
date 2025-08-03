using Markov.Services.Enums;
using Markov.Services.Interfaces;

public class BacktestParameters
{
      public string Symbol { get; set; }
        public TimeFrame TimeFrame { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal InitialCapital { get; set; }
        public IExchange Exchange { get; set; }
}