using Moq;
using FluentAssertions;
using Markov.Services.Engine;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Markov.Services.Enums;
using Markov.Services.Time;

namespace Markov.Tests.Engine
{
    public class TradingEngineTests
    {
        private readonly Mock<IExchange> _mockExchange;
        private readonly Mock<IStrategy> _mockStrategy;
        private readonly Mock<ITimerService> _mockTimerService;
        private readonly List<string> _symbols;
        private readonly TradingEngine _tradingEngine;

        public TradingEngineTests()
        {
            _mockExchange = new Mock<IExchange>();
            _mockStrategy = new Mock<IStrategy>();
            _mockTimerService = new Mock<ITimerService>();
            _symbols = new List<string> { "BTCUSDT" };

            _tradingEngine = new TradingEngine(
                _mockExchange.Object,
                _mockStrategy.Object,
                _symbols,
                TimeFrame.OneDay,
                _mockTimerService.Object // Injected
            );

            // Setup the mock timer to complete immediately
            _mockTimerService.Setup(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                             .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task StartAsync_ShouldFetchDataAndGenerateSignals()
        {
            var signals = new List<Signal> { new Signal { Symbol = "BTCUSDT", Type = SignalType.Buy } };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync(new List<Candle>());
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()))
                         .Returns(signals);
            _mockExchange.Setup(e => e.PlaceOrderAsync(It.IsAny<Order>()))
                         .ReturnsAsync(new Order());

            await _tradingEngine.StartAsync();
            await Task.Delay(100); // Allow some time for the background task to run
            await _tradingEngine.StopAsync();

            _mockExchange.Verify(e => e.GetHistoricalDataAsync("BTCUSDT", It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.AtLeastOnce());
            _mockExchange.Verify(e => e.GetHistoricalDataAsync("ETHUSDT", It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.AtLeastOnce());
            _mockStrategy.Verify(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task StartAsync_WhenSignalIsGenerated_ShouldPlaceOrder()
        {
            var signal = new Signal { Symbol = "BTCUSDT", Type = SignalType.Buy, Price = 50000 };
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()))
                         .Returns(new List<Signal> { signal });
            _mockExchange.Setup(e => e.PlaceOrderAsync(It.IsAny<Order>())).ReturnsAsync(new Order());

            await _tradingEngine.StartAsync();
            await Task.Delay(50); // Just a tiny delay to let the task start
            await _tradingEngine.StopAsync();

            _mockExchange.Verify(e => e.PlaceOrderAsync(It.Is<Order>(o => o.Symbol == signal.Symbol)), Times.AtLeastOnce());
        }
        [Fact]
        public async Task OnOrderPlaced_EventShouldFire_WhenOrderIsPlaced()
        {
            var placedOrder = new Order { Id = "123" };
            Order receivedOrder = null;
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()))
                         .Returns(new List<Signal> { new Signal() });
            _mockExchange.Setup(e => e.PlaceOrderAsync(It.IsAny<Order>())).ReturnsAsync(placedOrder);
            _tradingEngine.OnOrderPlaced += (order) => receivedOrder = order;

            await _tradingEngine.StartAsync();
            await Task.Delay(50);
            await _tradingEngine.StopAsync();

            receivedOrder?.Id.Should().Be("123");
        }

        [Fact]
        public async Task StopAsync_ShouldCancelTheRunningTask()
        {
            var hasLoopContinued = false;
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync(new List<Candle>())
                         .Callback(async () =>
                         {
                             await _tradingEngine.StopAsync();
                         });
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()))
                         .Returns(new List<Signal>())
                         .Callback(() =>
                         {
                             // This callback should not be reached the second time if cancellation is working
                             if (hasLoopContinued) throw new Exception("Loop was not cancelled.");
                             hasLoopContinued = true;
                         });

            Func<Task> act = () => _tradingEngine.StartAsync();

            await act.Should().NotThrowAsync();
        }
    }
}