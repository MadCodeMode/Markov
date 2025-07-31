using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Linq;

namespace Markov.Services.Services;

public class BinanceDataFetcher : ICryptoDataFetcher
{
    private readonly BinanceRestClient _client;
    private readonly ILogger<BinanceDataFetcher> _logger;
    public BinanceDataFetcher(
        IOptions<BinanceSettings> settings,
        ILogger<BinanceDataFetcher> logger)
    {
        if (string.IsNullOrWhiteSpace(settings.Value.ApiKey) || string.IsNullOrWhiteSpace(settings.Value.ApiSecret))
        {
            throw new ArgumentException("Binance API key and secret must be configured.");
        }

        _logger = logger; 

        _client = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(settings.Value.ApiKey, settings.Value.ApiSecret);
        });
    }

    public async Task<Asset> FetchDataAsync(string assetName, DateTime from, DateTime to)
    {
        
        var klines = await _client.SpotApi.ExchangeData.GetKlinesAsync(assetName, KlineInterval.OneDay, from, to);
        
        _logger.LogInformation("{startDate} - {endDate}", from, to);
        var ordered = klines.Data.OrderByDescending(x => x.OpenTime).ToArray();

        _logger.LogInformation("{first} - {last}", ordered[0].OpenTime, ordered[ordered.Length - 1].OpenTime);


        var candles = klines.Data
            .Select(k => new Candle
            {
                Timestamp = k.OpenTime,
                Movement = k.ClosePrice > k.OpenPrice ? Movement.Up : Movement.Down,
                Open = k.OpenPrice,
                Close = k.ClosePrice,
                High = k.HighPrice,
                Low = k.LowPrice,
                TradeCount = k.TradeCount,
                Volume = k.Volume
            })
            .ToList();

        return new Asset
        {
            Name = assetName,
            HistoricalData = candles,
            Source = nameof(BinanceDataFetcher),
            AssetType = AssetType.Crypto
        };
    }
}
