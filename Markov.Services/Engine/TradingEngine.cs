using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Markov.Services.Time;
using Microsoft.Extensions.Logging;

namespace Markov.Services.Engine
{
    public class TradingEngine : ITradingEngine
    {
        public event Action<Order>? OnOrderPlaced;

        private readonly IExchange _exchange;
        private readonly IStrategy _strategy;
        private readonly IEnumerable<string> _symbols;
        private readonly TimeFrame _timeFrame;
        private readonly ITimerService _timerService;
        private readonly ILogger<TradingEngine> _logger;
        private readonly TimeSpan _tradingLoopInterval;

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _tradingLoopTask;

        public TradingEngine(
            IExchange exchange,
            IStrategy strategy,
            IEnumerable<string> symbols,
            TimeFrame timeFrame,
            ITimerService timerService,
            ILogger<TradingEngine> logger,
            TimeSpan? tradingLoopInterval = null)
        {
            _exchange = exchange;
            _strategy = strategy;
            _symbols = symbols;
            _timeFrame = timeFrame;
            _timerService = timerService;
            _logger = logger;
            _tradingLoopInterval = tradingLoopInterval ?? TimeSpan.FromSeconds(10);
        }

        public Task StartAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            _tradingLoopTask = Task.Run(() => TradingLoopAsync(token), token);
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            if (_tradingLoopTask != null)
            {
                await _tradingLoopTask;
            }
        }

        private async Task TradingLoopAsync(CancellationToken token)
        {
            try
            {
                var data = await InitializeHistoricalDataAsync();

                while (!token.IsCancellationRequested)
                {
                    await _timerService.Delay(_tradingLoopInterval, token);

                    await FetchLatestDataAsync(data);

                    var signals = _strategy.GetFilteredSignals(data);

                    foreach (var signal in signals)
                    {
                        var order = new Order
                        {
                            Symbol = signal.Symbol,
                            Side = signal.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell,
                            Type = OrderType.Market,
                            Quantity = 1, // Simplified for now
                            Price = signal.Price,
                            Timestamp = DateTime.UtcNow
                        };

                        // Apply hold strategy: only set SL/TP if UseHoldStrategy is false
                        if (!signal.UseHoldStrategy)
                        {
                            order.StopLoss = signal.StopLoss;
                            order.TakeProfit = signal.TakeProfit;
                        }
                        // For long trades with UseHoldStrategy, SL is intentionally omitted.

                        var placedOrder = await _exchange.PlaceOrderAsync(order);
                        OnOrderPlaced?.Invoke(placedOrder);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // This is expected on shutdown, so we can ignore it.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in the trading loop.");
            }
        }

        private async Task<Dictionary<string, IEnumerable<Candle>>> InitializeHistoricalDataAsync()
        {
            var data = new Dictionary<string, IEnumerable<Candle>>();
            _logger.LogInformation("Initializing historical data...");
            foreach (var symbol in _symbols)
            {
                var historicalData = await _exchange.GetHistoricalDataAsync(symbol, _timeFrame, DateTime.UtcNow.AddDays(-100), DateTime.UtcNow);
                data.Add(symbol, new List<Candle>(historicalData));
                _logger.LogInformation($"Fetched {((List<Candle>)data[symbol]).Count} initial candles for {symbol}.");
            }
            return data;
        }

        private async Task FetchLatestDataAsync(Dictionary<string, IEnumerable<Candle>> data)
        {
            _logger.LogDebug("Fetching latest candle data...");
            // Fetch a small, recent window to get the latest candle(s).
            var since = DateTime.UtcNow.AddMinutes(-2 * (int)_timeFrame);

            foreach (var symbol in _symbols)
            {
                var latestCandles = await _exchange.GetHistoricalDataAsync(symbol, _timeFrame, since, DateTime.UtcNow);
                var candleList = (List<Candle>)data[symbol];
                var lastTimestamp = candleList.LastOrDefault()?.Timestamp;

                foreach (var newCandle in latestCandles)
                {
                    if (lastTimestamp == null || newCandle.Timestamp > lastTimestamp)
                    {
                        candleList.Add(newCandle);
                        _logger.LogDebug($"Added new candle for {symbol} at {newCandle.Timestamp}.");
                    }
                    else if (newCandle.Timestamp == lastTimestamp)
                    {
                        // Update the last candle if it's still open
                        candleList[candleList.Count - 1] = newCandle;
                    }
                }
            }
        }
    }
}
