using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Exchanges;

public class DummyExchange : IExchange
{
    public Task CancelOrderAsync(string orderId) => Task.CompletedTask;
    public Task<Order> GetOrderAsync(string orderId) => throw new NotImplementedException();
    public Task<Order> PlaceOrderAsync(Order order) => Task.FromResult(order);

    public Task<IEnumerable<Candle>> GetHistoricalDataAsync(string symbol, TimeFrame timeFrame, DateTime from, DateTime to)
    {
        var candles = new List<Candle>();
        var date = from;
        var random = new Random(12345); // Use a fixed seed for consistent results
        decimal price = 25000;

        while (date < to)
        {
            var open = price;
            var change = (decimal)(random.NextDouble() * 600 - 300);
            var close = open + change;
            var high = Math.Max(open, close) + (decimal)(random.NextDouble() * 100);
            var low = Math.Min(open, close) - (decimal)(random.NextDouble() * 100);
            candles.Add(new Candle { Timestamp = date, Open = open, High = high, Low = low, Close = close, Volume = (decimal)random.NextDouble() * 1000 });
            price = close > 0 ? close : 1;
            date = date.AddDays(1);
        }
        return Task.FromResult(candles.AsEnumerable());
    }
}