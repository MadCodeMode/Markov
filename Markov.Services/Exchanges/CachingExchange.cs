using System.Text.Json;
using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Exchanges;

public class CachingExchange : IExchange
{
    private readonly IExchange _exchange;
    private readonly string _cacheDirectory;

    public CachingExchange(IExchange exchange)
    {
        _exchange = exchange;
        _cacheDirectory = Path.Combine(AppContext.BaseDirectory, "Cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<IEnumerable<Candle>> GetHistoricalDataAsync(string symbol, TimeFrame timeFrame, DateTime from, DateTime to)
    {
        var fileName = GetCacheFileName(symbol, timeFrame, from, to);
        var filePath = Path.Combine(_cacheDirectory, fileName);

        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            var candles = JsonSerializer.Deserialize<List<Candle>>(json);
            if (candles != null && candles.Any() && candles.First().Timestamp <= from && candles.Last().Timestamp >= to)
            {
                Console.WriteLine($"Loading {symbol} data from cache.");
                return candles.Where(c => c.Timestamp >= from && c.Timestamp <= to);
            }
        }

        Console.WriteLine($"Fetching {symbol} data from exchange.");
        var freshData = await _exchange.GetHistoricalDataAsync(symbol, timeFrame, from, to);
        var freshDataList = freshData.ToList();

        if (freshDataList.Any())
        {
            var json = JsonSerializer.Serialize(freshDataList);
            await File.WriteAllTextAsync(filePath, json);
        }

        return freshDataList;
    }

    public Task<Order> PlaceOrderAsync(Order order)
    {
        return _exchange.PlaceOrderAsync(order);
    }

    public Task<Order> GetOrderAsync(string orderId)
    {
        return _exchange.GetOrderAsync(orderId);
    }

    public Task CancelOrderAsync(string orderId)
    {
        return _exchange.CancelOrderAsync(orderId);
    }

    private string GetCacheFileName(string symbol, TimeFrame timeFrame, DateTime from, DateTime to)
    {
        return $"{symbol}_{timeFrame}_{from:yyyyMMdd}_{to:yyyyMMdd}.json";
    }
}