using Markov.Services.Models;

namespace Markov.Services.Interfaces;

    public interface IIndicator
    {
        string Name { get; }
        IEnumerable<decimal> Calculate(IEnumerable<Candle> candles);
    }