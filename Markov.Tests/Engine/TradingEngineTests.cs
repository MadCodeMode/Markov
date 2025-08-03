using Moq;
using FluentAssertions;
using Markov.Services.Engine;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Markov.Services.Enums;
using Markov.Services.Time;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Markov.Tests.Engine
{
    public class TradingEngineTests
    {
        private readonly Mock<IExchange> _mockExchange;
        private readonly Mock<IStrategy> _mockStrategy;
        private readonly Mock<ITimerService> _mockTimerService;
        private readonly Mock<ILogger<TradingEngine>> _mockLogger;
        private readonly List<string> _symbols;
        private readonly TradingEngine _tradingEngine;

        public TradingEngineTests()
        {
            _mockExchange = new Mock<IExchange>();
            _mockStrategy = new Mock<IStrategy>();
            _mockTimerService = new Mock<ITimerService>();
            _mockLogger = new Mock<ILogger<TradingEngine>>();
            _symbols = new List<string> { "BTCUSDT" };

            _tradingEngine = new TradingEngine(
                _mockExchange.Object,
                _mockStrategy.Object,
                _symbols,
                TimeFrame.OneMinute,
                _mockTimerService.Object,
                _mockLogger.Object,
                TimeSpan.FromMilliseconds(10) // Use a short interval for testing
            );

            // Setup mocks for successful runs
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync(new List<Candle> { new Candle { Timestamp = DateTime.UtcNow } });
            _mockTimerService.Setup(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                             .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task StartAsync_ShouldInitializeDataAndRunLoop()
        {
            // Arrange
            var signals = new List<Signal> { new Signal { Symbol = "BTCUSDT", Type = SignalType.Buy } };
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()))
                         .Returns(signals);
            _mockExchange.Setup(e => e.PlaceOrderAsync(It.IsAny<Order>()))
                         .ReturnsAsync(new Order());

            // Act
            await _tradingEngine.StartAsync();
            await Task.Delay(100); // Allow some time for the loop to run
            await _tradingEngine.StopAsync();

            // Assert
            // Verify initial historical data fetch
            _mockExchange.Verify(e => e.GetHistoricalDataAsync("BTCUSDT", It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.AtLeastOnce());
            // Verify that the strategy was called
            _mockStrategy.Verify(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task WhenSignalGenerated_ShouldPlaceOrderAndFireEvent()
        {
            // Arrange
            var signal = new Signal { Symbol = "BTCUSDT", Type = SignalType.Buy, Price = 50000 };
            var placedOrder = new Order { Id = "123", Symbol = "BTCUSDT" };
            Order? receivedOrder = null;

            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()))
                         .Returns(new List<Signal> { signal });
            _mockExchange.Setup(e => e.PlaceOrderAsync(It.Is<Order>(o => o.Symbol == signal.Symbol)))
                         .ReturnsAsync(placedOrder);
            _tradingEngine.OnOrderPlaced += (order) => receivedOrder = order;

            // Act
            await _tradingEngine.StartAsync();
            await Task.Delay(100);
            await _tradingEngine.StopAsync();

            // Assert
            _mockExchange.Verify(e => e.PlaceOrderAsync(It.Is<Order>(o => o.Symbol == signal.Symbol)), Times.AtLeastOnce());
            receivedOrder.Should().NotBeNull();
            receivedOrder!.Id.Should().Be(placedOrder.Id);
        }

        [Fact]
        public async Task StopAsync_ShouldCancelTheRunningTaskGracefully()
        {
            // Arrange
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var callCount = 0;

            _mockTimerService.Setup(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback((TimeSpan ts, CancellationToken ct) => {
                    callCount++;
                    if(callCount == 1)
                    {
                        tcs.TrySetResult();
                    }
                })
                .Returns(async (TimeSpan ts, CancellationToken ct) => {
                    var tcs = new TaskCompletionSource<bool>();
                    using (ct.Register(() => tcs.SetResult(true)))
                    {
                        await tcs.Task;
                    }
                });

            // Act
            await _tradingEngine.StartAsync();
            await tcs.Task; // Wait for the first loop to start its delay
            await _tradingEngine.StopAsync();

            // Give a moment for any potential extra calls to come through
            await Task.Delay(50);

            // Assert
            callCount.Should().Be(1); // It should be called once, but not again after stopping.
        }

        [Fact]
        public async Task WhenSignalHasHoldStrategy_ShouldPlaceOrderWithoutStopLoss()
        {
            // Arrange
            var signal = new Signal 
            { 
                Symbol = "BTCUSDT", 
                Type = SignalType.Buy, 
                Price = 50000, 
                StopLoss = 45000, // A stop loss is provided
                UseHoldStrategy = true // But the hold strategy is active
            };
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()))
                         .Returns(new List<Signal> { signal });
            _mockExchange.Setup(e => e.PlaceOrderAsync(It.IsAny<Order>()))
                         .ReturnsAsync(new Order());

            // Act
            await _tradingEngine.StartAsync();
            await Task.Delay(100);
            await _tradingEngine.StopAsync();

            // Assert
            // Verify that the placed order has a null StopLoss because the hold strategy was on.
            _mockExchange.Verify(e => e.PlaceOrderAsync(It.Is<Order>(o => 
                o.Symbol == signal.Symbol && 
                o.StopLoss == null)), 
                Times.AtLeastOnce());
        }

        [Fact]
        public async Task WhenInitialFetchFails_ShouldLogAndNotLoop()
        {
            // Arrange
            var exception = new InvalidOperationException("Exchange is down");
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(exception);

            // Act
            await _tradingEngine.StartAsync();
            await Task.Delay(100); // Let the loop attempt to run
            await _tradingEngine.StopAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("The TradingEngine failed to initialize")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            // Verify the loop did not continue
            _mockTimerService.Verify(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    [Fact]
        public async Task WhenUpdateFails_ShouldLogAndContinueLoop()
        {
            // Arrange
            var exception = new InvalidOperationException("Exchange is down for an update");
            var initialCandles = new List<Candle> { new Candle { Timestamp = DateTime.UtcNow.AddHours(-1) } };
            var callCount = 0;

            // Succeed on the first call (initial fetch)
            _mockExchange.SetupSequence(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(initialCandles)
                .ThrowsAsync(exception); // Fail on the second call (update fetch)

            _mockTimerService.Setup(t => t.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback(() => callCount++)
                .Returns(Task.CompletedTask);

            // Act
            await _tradingEngine.StartAsync();
            await Task.Delay(100); // Allow time for a few loops
            await _tradingEngine.StopAsync();

            // Assert
            // Verify that the exception was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unexpected error occurred")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Verify that the loop continued after the exception
            callCount.Should().BeGreaterThan(1);
        }
    }
}
