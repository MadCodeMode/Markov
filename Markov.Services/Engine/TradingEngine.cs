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
        private readonly TradingSettings _tradingSettings;

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _tradingLoopTask;
        private readonly Dictionary<string, Order> _openPositions = new Dictionary<string, Order>();
        // Note: For production environments, a persistent data store (e.g., a database) 
        // is recommended over in-memory storage to prevent data loss on application restart.
        private readonly Dictionary<string, List<Candle>> _data = new Dictionary<string, List<Candle>>();
        private readonly object _dataLock = new object();

        public TradingEngine(
            IExchange exchange,
            IStrategy strategy,
            IEnumerable<string> symbols,
            TimeFrame timeFrame,
            ITimerService timerService,
            ILogger<TradingEngine> logger,
            TradingSettings tradingSettings)
        {
            _exchange = exchange;
            _strategy = strategy;
            _symbols = symbols;
            _timeFrame = timeFrame;
            _timerService = timerService;
            _logger = logger;
            _tradingSettings = tradingSettings;
            _tradingLoopInterval = TimeSpan.FromSeconds(_tradingSettings.TradingLoopIntervalSeconds);
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
                // Initial data fetch. If this fails, the engine should not start.
                await UpdateHistoricalDataAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The TradingEngine failed to initialize and will not start.");
                return;
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Subsequent updates are within the loop's try-catch.
                    await UpdateHistoricalDataAsync(token);

                    var signals = _strategy.GetFilteredSignals(_data
                        .ToDictionary(kvp => kvp.Key, kvp => (IEnumerable<Candle>)kvp.Value));

                    var lastCandleTimestamps = _data
                        .Where(kvp => kvp.Value.Any())
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Last().Timestamp);

                    var recentSignals = signals.Where(s => 
                        lastCandleTimestamps.ContainsKey(s.Symbol) && 
                        s.Timestamp == lastCandleTimestamps[s.Symbol]);

                    if (recentSignals.Any())
                    {
                        _logger.LogInformation($"Processing {recentSignals.Count()} new signal(s).");
                    }

                    foreach (var signal in recentSignals)
                    {
                        var positionExists = _openPositions.ContainsKey(signal.Symbol);

                        if (signal.Type == SignalType.Buy && !positionExists)
                        {
                            // Open a new position
                            var order = await CreateOrderFromSignal(signal, token);
                            if (order != null)
                            {
                                var placedOrder = await _exchange.PlaceOrderAsync(order);
                                OnOrderPlaced?.Invoke(placedOrder);
                                _openPositions[signal.Symbol] = placedOrder;
                                _logger.LogInformation($"Opened position for {signal.Symbol}.");
                            }
                        }
                        else if (signal.Type == SignalType.Sell && positionExists)
                        {
                            // Close an existing position
                            var order = await CreateOrderFromSignal(signal, token);
                            if (order != null)
                            {
                                var placedOrder = await _exchange.PlaceOrderAsync(order);
                                OnOrderPlaced?.Invoke(placedOrder);
                                _openPositions.Remove(signal.Symbol);
                                _logger.LogInformation($"Closed position for {signal.Symbol}.");
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Trading loop was canceled.");
                    break;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Trading loop was canceled.");
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "An operation failed, likely due to an issue with the exchange API.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected error occurred in the trading loop. The loop will continue.");
                }
                
                await _timerService.Delay(_tradingLoopInterval, token);
            }
        }

        private async Task<Order?> CreateOrderFromSignal(Signal signal, CancellationToken token)
        {
            decimal quantity;
            if (_tradingSettings.SizeMode == TradeSizeMode.FixedAmount)
            {
                quantity = _tradingSettings.Size;
            }
            else // PercentageOfCapital
            {
                var balance = await _exchange.GetBalanceAsync("USDT", token);
                var capital = balance.Free;
                if (signal.Price > 0)
                {
                    quantity = (capital * (_tradingSettings.Size / 100m)) / signal.Price;
                }
                else
                {
                    _logger.LogWarning("Signal price is 0, cannot calculate quantity. Skipping order.");
                    return null;
                }
            }

            var order = new Order
            {
                Symbol = signal.Symbol,
                Side = signal.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell,
                Type = OrderType.Market,
                Quantity = quantity,
                Price = signal.Price,
                Timestamp = DateTime.UtcNow,
                UseHoldStrategy = signal.UseHoldStrategy
            };

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

            return order;
        }

        private async Task UpdateHistoricalDataAsync(CancellationToken token)
        {
            foreach (var symbol in _symbols)
            {
                DateTime fromDate;
                bool isInitialFetch;

                lock (_dataLock)
                {
                    isInitialFetch = !_data.ContainsKey(symbol) || !_data[symbol].Any();
                    if (isInitialFetch)
                    {
                        fromDate = DateTime.UtcNow.AddDays(-100); // Large window for initial fetch
                        _data[symbol] = new List<Candle>();
                        _logger.LogInformation($"Performing initial data fetch for {symbol}...");
                    }
                    else
                    {
                        // Fetch data since the last candle, adding a 1-second buffer.
                        fromDate = _data[symbol].Max(c => c.Timestamp).AddSeconds(1);
                    }
                }

                var newCandles = await _exchange.GetHistoricalDataAsync(symbol, _timeFrame, fromDate, DateTime.UtcNow, token);

                if (newCandles.Any())
                {
                    lock (_dataLock)
                    {
                        var existingCandles = _data[symbol].ToHashSet();
                        var addedCount = 0;
                        foreach (var candle in newCandles)
                        {
                            if (existingCandles.Add(candle))
                            {
                                _data[symbol].Add(candle);
                                addedCount++;
                            }
                        }
                        if (addedCount > 0)
                        {
                            _data[symbol] = _data[symbol].OrderBy(c => c.Timestamp).ToList();
                            _logger.LogInformation($"Fetched and added {addedCount} new candle(s) for {symbol}.");
                        }
                    }
                }
            }
        }
    }
}
