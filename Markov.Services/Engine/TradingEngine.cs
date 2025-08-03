using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Markov.Services.Time;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Markov.Services.Engine
{
    public class TradingEngine : ITradingEngine
    {
        public event Action<Order> OnOrderPlaced;

        private readonly IExchange _exchange;
        private readonly IStrategy _strategy;
        private readonly IEnumerable<string> _symbols;
        private readonly TimeFrame _timeFrame;
        private readonly ITimerService _timerService;
        private CancellationTokenSource _cancellationTokenSource;

        public TradingEngine(
            IExchange exchange,
            IStrategy strategy,
            IEnumerable<string> symbols,
            TimeFrame timeFrame,
            ITimerService timerService)
        {
            _exchange = exchange;
            _strategy = strategy;
            _symbols = symbols;
            _timeFrame = timeFrame;
            _timerService = timerService;
        }

        public Task StartAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var data = new Dictionary<string, IEnumerable<Candle>>();
                        foreach (var symbol in _symbols)
                        {
                            var historicalData = await _exchange.GetHistoricalDataAsync(symbol, _timeFrame, DateTime.UtcNow.AddDays(-100), DateTime.UtcNow);
                            data.Add(symbol, historicalData);
                        }

                        var signals = _strategy.GetFilteredSignals(data);

                        foreach (var signal in signals)
                        {
                            var order = new Order
                            {
                                Symbol = signal.Symbol,
                                Side = signal.Type == SignalType.Buy ? OrderSide.Buy : OrderSide.Sell,
                                Type = OrderType.Market,
                                Quantity = 1,
                                Price = signal.Price,
                                StopLoss = signal.StopLoss,
                                TakeProfit = signal.TakeProfit,
                                UseHoldStrategy = signal.UseHoldStrategy,
                                Timestamp = DateTime.UtcNow
                            };

                            var placedOrder = await _exchange.PlaceOrderAsync(order);
                            OnOrderPlaced?.Invoke(placedOrder);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log exception
                    }

                    // In a real scenario, the delay would be calculated more precisely
                    // to align with the start of the next candle.
                    await _timerService.Delay(TimeSpan.FromSeconds(10), token);
                }
            }, token);
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }
    }
}