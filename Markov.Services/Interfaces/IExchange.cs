using Markov.Services.Enums;
using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public interface IExchange
{
    Task<IEnumerable<Candle>> GetHistoricalDataAsync(string symbol, TimeFrame timeFrame, DateTime from, DateTime to);
    Task<Order> PlaceOrderAsync(Order order);
    Task<Order> GetOrderAsync(string orderId);
    Task CancelOrderAsync(string orderId);
}