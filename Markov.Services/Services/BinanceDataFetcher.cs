using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Microsoft.Extensions.Options;

namespace Markov.Services.Services;

public class BinanceDataFetcher : ICryptoDataFetcher
{
    private readonly BinanceRestClient _client;
    public BinanceDataFetcher(IOptions<BinanceSettings> settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Value.ApiKey) || string.IsNullOrWhiteSpace(settings.Value.ApiSecret))
        {
            throw new ArgumentException("Binance API key and secret must be configured.");
        }

        _client = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(settings.Value.ApiKey, settings.Value.ApiSecret);
        });
    }

    public async Task<Asset> FetchDataAsync(string assetName, DateTime from, DateTime to)
    {
        var klines = await _client.SpotApi.ExchangeData.GetKlinesAsync(assetName, Binance.Net.Enums.KlineInterval.OneDay, from, to);

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
