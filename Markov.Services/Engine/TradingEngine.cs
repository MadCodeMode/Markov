using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Engine;

public class TradingEngine : ITradingEngine
{
    private readonly IExchange _exchange;
    private readonly IStrategy _strategy;
    private readonly IEnumerable<string> _symbols;
    private readonly TimeFrame _timeFrame;
    private CancellationTokenSource _cancellationTokenSource;

    public TradingEngine(IExchange exchange, IStrategy strategy, IEnumerable<string> symbols, TimeFrame timeFrame)
    {
        _exchange = exchange;
        _strategy = strategy;
        _symbols = symbols;
        _timeFrame = timeFrame;
    }

    public Task StartAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        return Task.Run(async () =>
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
                            // Pass the new risk management parameters to the order
                            Price = signal.Price,
                            StopLoss = signal.StopLoss,
                            TakeProfit = signal.TakeProfit,
                            UseHoldStrategy = signal.UseHoldStrategy
                        };
                        await _exchange.PlaceOrderAsync(order);
                    }
                }
                catch (Exception ex)
                {
                    // Log exception
                }

                await Task.Delay(TimeSpan.FromMinutes((int)_timeFrame), token);
            }
        }, token);
    }

    public Task StopAsync()
    {
        _cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }
}