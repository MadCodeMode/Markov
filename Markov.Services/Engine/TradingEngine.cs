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
                var data = new Dictionary<string, List<Candle>>();

                while (!token.IsCancellationRequested)
                {
                    await UpdateHistoricalDataAsync(data);

                    var signals = _strategy.GetFilteredSignals(data.ToDictionary(kvp => kvp.Key, kvp => (IEnumerable<Candle>)kvp.Value));

                    foreach (var signal in signals)
                    {
                        var order = new Order
                        {
                            Symbol = signal.Symbol,
                            Side = signal.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell,
                            Type = OrderType.Market,
                            Quantity = 1, // Simplified for now
                            Price = signal.Price,
                            Timestamp = DateTime.UtcNow,
                            UseHoldStrategy = signal.UseHoldStrategy
                        };

                        // Apply hold strategy: only set SL if UseHoldStrategy is false for Buy orders
                        if (signal.Type == SignalType.Buy && signal.UseHoldStrategy)
                        {
                            order.StopLoss = null;
                            order.TakeProfit = signal.TakeProfit;
                        }
                        else
                        {
                            order.StopLoss = signal.StopLoss;
                            order.TakeProfit = signal.TakeProfit;
                        }

                        var placedOrder = await _exchange.PlaceOrderAsync(order);
                        OnOrderPlaced?.Invoke(placedOrder);
                    }
                    
                    await _timerService.Delay(_tradingLoopInterval, token);
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

        private async Task UpdateHistoricalDataAsync(Dictionary<string, List<Candle>> data)
        {
            foreach (var symbol in _symbols)
            {
                DateTime fromDate;
                bool isInitialFetch = !data.ContainsKey(symbol) || !data[symbol].Any();

                if (isInitialFetch)
                {
                    fromDate = DateTime.UtcNow.AddDays(-100); // Large window for initial fetch
                    data[symbol] = new List<Candle>();
                    _logger.LogInformation($"Performing initial data fetch for {symbol}...");
                }
                else
                {
                    // Fetch data since the last candle, adding a 1-second buffer.
                    fromDate = data[symbol].Max(c => c.Timestamp).AddSeconds(1);
                }

                var newCandles = await _exchange.GetHistoricalDataAsync(symbol, _timeFrame, fromDate, DateTime.UtcNow);

                if (newCandles.Any())
                {
                    var existingCandles = data[symbol].ToHashSet();
                    var addedCount = 0;
                    foreach (var candle in newCandles)
                    {
                        if (existingCandles.Add(candle))
                        {
                            data[symbol].Add(candle);
                            addedCount++;
                        }
                    }
                    if (addedCount > 0)
                    {
                        data[symbol] = data[symbol].OrderBy(c => c.Timestamp).ToList();
                        _logger.LogInformation($"Fetched and added {addedCount} new candle(s) for {symbol}.");
                    }
                }
            }
        }
    }
}
