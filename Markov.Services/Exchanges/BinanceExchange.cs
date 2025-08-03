using Binance.Net.Clients;
using Binance.Net.Enums;
using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Microsoft.Extensions.Options;

namespace Markov.Services.Exchanges
{
    public class BinanceExchange : IExchange
    {
        private readonly BinanceRestClient _client;

        public BinanceExchange(IOptions<BinanceSettings> settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Value.ApiKey) || string.IsNullOrWhiteSpace(settings.Value.ApiSecret))
            {
                // For public data endpoints, credentials are not strictly necessary,
                // but they are required for trading and increase rate limits.
                // We will use anonymous credentials for now.
                _client = new BinanceRestClient();
            }
            else
            {
                _client = new BinanceRestClient(options =>
                {
                    options.ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(settings.Value.ApiKey, settings.Value.ApiSecret);
                });
            }
        }

        public async Task<IEnumerable<Candle>> GetHistoricalDataAsync(string symbol, TimeFrame timeFrame, DateTime from, DateTime to)
        {
            var klineInterval = ConvertTimeFrameToKlineInterval(timeFrame);
            var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(symbol, klineInterval, from, to, 1000);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to fetch data from Binance: {result.Error?.Message ?? "Unknown error"}");
            }

            return result.Data.Select(k => new Candle
            {
                Timestamp = k.OpenTime,
                Open = k.OpenPrice,
                High = k.HighPrice,
                Low = k.LowPrice,
                Close = k.ClosePrice,
                Volume = k.Volume
            });
        }

        public Task<Order> PlaceOrderAsync(Order order)
        {
            // This will be implemented in a future story.
            throw new NotImplementedException();
        }

        public Task<Order> GetOrderAsync(string orderId)
        {
            throw new NotImplementedException();
        }

        public Task CancelOrderAsync(string orderId)
        {
            throw new NotImplementedException();
        }

        private static KlineInterval ConvertTimeFrameToKlineInterval(TimeFrame timeFrame)
        {
            return timeFrame switch
            {
                TimeFrame.OneMinute => KlineInterval.OneMinute,
                TimeFrame.FiveMinutes => KlineInterval.FiveMinutes,
                TimeFrame.FifteenMinutes => KlineInterval.FifteenMinutes,
                TimeFrame.OneHour => KlineInterval.OneHour,
                TimeFrame.FourHours => KlineInterval.FourHour,
                TimeFrame.OneDay => KlineInterval.OneDay,
                TimeFrame.OneWeek => KlineInterval.OneWeek,
                TimeFrame.OneMonth => KlineInterval.OneMonth,
                _ => throw new ArgumentOutOfRangeException(nameof(timeFrame), $"Not supported time frame: {timeFrame}")
            };
        }
    }
}