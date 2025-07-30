using Markov.Core.Interfaces;
using Markov.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using YahooFinanceApi;

namespace Markov.Core.Services;

public class YahooDataFetcher : IStockDataFetcher
{
    public async Task<Asset> FetchDataAsync(string assetName, DateTime from, DateTime to)
    {
        var historicalData = await Yahoo.GetHistoricalAsync(assetName, from, to);
        var candles = historicalData
            .Select(c => new Models.Candle
            {
                Timestamp = c.DateTime,
                Movement = c.Close > c.Open ? Movement.Up : Movement.Down
            })
            .ToList();

        return new Asset
        {
            Name = assetName,
            HistoricalData = candles
        };
    }
}