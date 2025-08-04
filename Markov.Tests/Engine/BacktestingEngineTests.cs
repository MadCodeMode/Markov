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
using System.Threading;

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

        private BacktestParameters CreateDefaultParameters(decimal capital = 10000, decimal commission = 0, decimal slippage = 0)
        {
            return new BacktestParameters
            {
                Symbol = _symbol,
                TimeFrame = TimeFrame.OneDay,
                From = DateTime.UtcNow.AddDays(-10),
                To = DateTime.UtcNow,
                InitialCapital = capital,
                Exchange = _mockExchange.Object,
                CommissionPercentage = commission,
                SlippagePercentage = slippage,
                TradeSizeMode = TradeSizeMode.PercentageOfCapital,
                TradeSizeValue = 0.1m // 10%
            };
        }

        [Fact]
        public async Task RunAsync_LookaheadBiasFixed_ShouldEnterOnNextCandleOpen()
        {
            // Arrange
            var parameters = CreateDefaultParameters();
            var signalTime = parameters.From.AddDays(1);
            var entryTime = parameters.From.AddDays(2);
            var endTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime, Close = 100, Open = 95 }, // Signal generated on this candle
                new Candle { Timestamp = entryTime, Open = 102, Close = 110 },  // Entry should be at this candle's open
                new Candle { Timestamp = endTime, Open = 110, Close = 115 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = signalTime, Price = 100, Symbol = _symbol }
            };

            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            // Act
            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // Assert
            // Position is opened at 102 (next candle's open) and liquidated at 115 (last candle's close)
            // Trade size = 10000 * 0.1 = 1000
            // Quantity = 1000 / 102 = 9.8039
            // Liquidation value = 9.8039 * 115 = 1127.45
            // Final Capital = 9000 (remaining) + 1127.45 = 10127.45
            result.FinalCapital.Should().BeApproximately(10127.45m, 2);
            result.Trades.Should().BeEmpty(); // Position is liquidated, not closed as a trade
        }


        [Fact]
        public async Task RunAsync_ProfitableTrade_ShouldCalculateCorrectPnl()
        {
            var parameters = CreateDefaultParameters();
            var signalTime = parameters.From.AddDays(1);
            var entryTime = parameters.From.AddDays(2);
            var exitSignalTime = parameters.From.AddDays(2);
            var exitTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime, Close = 100 },                         // Signal to buy
                new Candle { Timestamp = entryTime, Open = 102, Close = 110 },             // Entry here, signal to sell
                new Candle { Timestamp = exitTime, Open = 112, Close = 120 }               // Exit here
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = signalTime, Price = 100, Symbol = _symbol },
                new Signal { Type = SignalType.Sell, Timestamp = exitSignalTime, Price = 110, Symbol = _symbol }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Calculation ---
            // Initial Capital: 10000
            // Trade Size: 1000
            // Entry Price: 102 (Open of next candle)
            // Exit Price: 112 (Open of candle after sell signal)
            // Capital after entry: 9000
            // Quantity: 1000 / 102 = 9.8039215686
            // Exit Value: 9.8039215686 * 112 = 1098.03921568
            // PNL: 1098.03921568 - 1000 = 98.03921568
            // Final Capital: 9000 + 1098.03921568 = 10098.03921568
            result.FinalCapital.Should().BeApproximately(10098.04m, 2);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().Pnl.Should().BeApproximately(98.04m, 2);
            result.WinCount.Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_LosingTrade_ShouldCalculateCorrectPnl()
        {
            var parameters = CreateDefaultParameters();
            var signalTime = parameters.From.AddDays(1);
            var entryTime = parameters.From.AddDays(2);
            var exitSignalTime = parameters.From.AddDays(2);
            var exitTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime, Close = 100 },
                new Candle { Timestamp = entryTime, Open = 102, Close = 95 }, // Entry here, signal to sell
                new Candle { Timestamp = exitTime, Open = 94, Close = 90 }   // Exit here
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = signalTime, Price = 100, Symbol = _symbol },
                new Signal { Type = SignalType.Sell, Timestamp = exitSignalTime, Price = 95, Symbol = _symbol }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Calculation ---
            // Initial Capital: 10000
            // Trade Size: 1000
            // Entry Price: 102
            // Exit Price: 94
            // Capital after entry: 9000
            // Quantity: 1000 / 102 = 9.8039215686
            // Exit Value: 9.8039215686 * 94 = 921.56862745
            // PNL: 921.56862745 - 1000 = -78.43137255
            // Final Capital: 9000 + 921.56862745 = 9921.56862745
            result.FinalCapital.Should().BeApproximately(9921.57m, 2);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().Pnl.Should().BeApproximately(-78.43m, 2);
            result.LossCount.Should().Be(1);
        }


        [Fact]
        public async Task RunAsync_TakeProfitHit_ShouldCloseAtTpPrice()
        {
            var parameters = CreateDefaultParameters();
            var signalTime = parameters.From.AddDays(1);
            var entryTime = parameters.From.AddDays(2);
            var exitTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime, Close = 100 },
                new Candle { Timestamp = entryTime, Open = 102, High = 105, Low = 101, Close = 104 }, // Entry
                new Candle { Timestamp = exitTime, Open = 105, High = 112, Low = 104, Close = 110 } // TP hit here
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = signalTime, Price = 100, Symbol = _symbol, TakeProfit = 110 }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Calculation ---
            // Entry Price: 102. TP: 110.
            // Trade Size: 1000. Quantity: 1000 / 102 = 9.8039215686
            // Exit Value: 9.8039215686 * 110 = 1078.43137255
            // PNL: 78.43137255
            // Final Capital: 9000 + 1078.43137255 = 10078.43137255
            result.FinalCapital.Should().BeApproximately(10078.43m, 2);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().ExitPrice.Should().Be(110);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.TakeProfit);
        }

        [Fact]
        public async Task RunAsync_StopLossHit_ShouldCloseAtSlPrice()
        {
            var parameters = CreateDefaultParameters();
            var signalTime = parameters.From.AddDays(1);
            var entryTime = parameters.From.AddDays(2);
            var exitTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime, Close = 100 },
                new Candle { Timestamp = entryTime, Open = 102, High = 105, Low = 101, Close = 104 }, // Entry
                new Candle { Timestamp = exitTime, Open = 105, High = 106, Low = 88, Close = 92 }   // SL hit here
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = signalTime, Price = 100, Symbol = _symbol, StopLoss = 90 }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Calculation ---
            // Entry Price: 102. SL: 90.
            // Trade Size: 1000. Quantity: 1000 / 102 = 9.8039215686
            // Exit Value: 9.8039215686 * 90 = 882.352941176
            // PNL: -117.647058824
            // Final Capital: 9000 + 882.352941176 = 9882.352941176
            result.FinalCapital.Should().BeApproximately(9882.35m, 2);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().ExitPrice.Should().Be(90);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.StopLoss);
        }

        [Fact]
        public async Task RunAsync_NoCandleData_ShouldReturnInitialCapital()
        {
            var parameters = CreateDefaultParameters();
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Candle>());
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>()))
                .Returns(new List<Signal>());

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.FinalCapital.Should().Be(parameters.InitialCapital);
            result.Trades.Should().BeEmpty();
        }

        [Fact]
        public async Task RunAsync_SignalOnLastCandle_ShouldNotOpenTrade()
        {
            var parameters = CreateDefaultParameters();
            var lastCandleTime = parameters.To.Date;
            var candles = new List<Candle>
            {
                new Candle { Timestamp = lastCandleTime.AddDays(-1), Open = 98, Close = 100 },
                new Candle { Timestamp = lastCandleTime, Open = 100, Close = 105 } // Signal here
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = lastCandleTime, Price = 105, Symbol = _symbol }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // No trade should be opened because there's no next candle to get the open price from.
            result.FinalCapital.Should().Be(10000);
            result.Trades.Should().BeEmpty();
        }
    }
}