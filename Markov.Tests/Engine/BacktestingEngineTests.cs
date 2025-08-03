using Xunit;
using Moq;
using FluentAssertions;
using Markov.Services.Engine;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Markov.Services.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Markov.Tests.Engine
{
    public class BacktestingEngineTests
    {
        private readonly Mock<IStrategy> _mockStrategy;
        private readonly Mock<IExchange> _mockExchange;
        private readonly BacktestingEngine _backtestingEngine;
        private readonly string _symbol = "BTCUSDT";

        public BacktestingEngineTests()
        {
            _mockStrategy = new Mock<IStrategy>();
            _mockExchange = new Mock<IExchange>();
            _backtestingEngine = new BacktestingEngine();
        }

        private BacktestParameters CreateDefaultParameters(decimal capital = 10000)
        {
            return new BacktestParameters
            {
                Symbol = _symbol,
                TimeFrame = TimeFrame.OneDay,
                From = DateTime.UtcNow.AddDays(-10),
                To = DateTime.UtcNow,
                InitialCapital = capital,
                Exchange = _mockExchange.Object
            };
        }

        [Fact]
        public async Task RunAsync_ProfitableTrade_ShouldCalculateCorrectPnl()
        {
            var parameters = CreateDefaultParameters();
            var entryTime = parameters.From.AddDays(1);
            var exitTime = parameters.From.AddDays(2);
            var candles = new List<Candle>
            {
                new Candle { Timestamp = entryTime, Close = 100 },
                new Candle { Timestamp = exitTime, Close = 110 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = entryTime, Price = 100, Symbol = _symbol },
                new Signal { Type = SignalType.Sell, Timestamp = exitTime, Price = 110, Symbol = _symbol }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.FinalCapital.Should().Be(11000);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().Pnl.Should().Be(1000);
            result.WinCount.Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_LosingTrade_ShouldCalculateCorrectPnl()
        {
            var parameters = CreateDefaultParameters();
            var entryTime = parameters.From.AddDays(1);
            var exitTime = parameters.From.AddDays(2);
            var candles = new List<Candle>
            {
                new Candle { Timestamp = entryTime, Close = 100 },
                new Candle { Timestamp = exitTime, Close = 90 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = entryTime, Price = 100, Symbol = _symbol },
                new Signal { Type = SignalType.Sell, Timestamp = exitTime, Price = 90, Symbol = _symbol }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.FinalCapital.Should().Be(9000);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().Pnl.Should().Be(-1000);
            result.LossCount.Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_TakeProfitHit_ShouldCloseAtTpPrice()
        {
            var parameters = CreateDefaultParameters();
            var entryTime = parameters.From.AddDays(1);
            var candles = new List<Candle>
            {
                new Candle { Timestamp = entryTime, Close = 100, High = 120, Low = 95 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = entryTime, Price = 100, Symbol = _symbol, TakeProfit = 110 }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.FinalCapital.Should().Be(11000);
            result.Trades.First().ExitPrice.Should().Be(110);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.TakeProfit);
        }

        [Fact]
        public async Task RunAsync_StopLossHit_ShouldCloseAtSlPrice()
        {
            var parameters = CreateDefaultParameters();
            var entryTime = parameters.From.AddDays(1);
            var candles = new List<Candle>
            {
                new Candle { Timestamp = entryTime, Close = 100, High = 105, Low = 85 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = entryTime, Price = 100, Symbol = _symbol, StopLoss = 90 }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.FinalCapital.Should().Be(9000);
            result.Trades.First().ExitPrice.Should().Be(90);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.StopLoss);
        }

        [Fact]
        public async Task RunAsync_HoldStrategy_ShouldMoveCapitalToHeldAssets()
        {
            var parameters = CreateDefaultParameters();
            var entryTime = parameters.From.AddDays(1);
            var candles = new List<Candle>
            {
                new Candle { Timestamp = entryTime, Close = 100 },
                new Candle { Timestamp = parameters.To.AddDays(-1), Close = 120 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = entryTime, Price = 100, Symbol = _symbol, UseHoldStrategy = true }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.FinalCapital.Should().Be(0);
            result.HeldAssets[_symbol].Should().Be(100);
            result.FinalHeldAssetsValue.Should().Be(12000);
            result.HoldCount.Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_OpenPositionAtEnd_ShouldLiquidateAtLastPrice()
        {
            var parameters = CreateDefaultParameters();
            var entryTime = parameters.From.AddDays(1);
            var candles = new List<Candle>
            {
                new Candle { Timestamp = entryTime, Close = 100 },
                new Candle { Timestamp = parameters.To.AddDays(-1), Close = 105 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = entryTime, Price = 100, Symbol = _symbol }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.FinalCapital.Should().Be(10500);
            result.Trades.Should().BeEmpty();
        }
    }
}