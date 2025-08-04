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
            // Capital after entry: 10000 - 1000 = 9000
            // Quantity = 1000 / 102 = 9.8039
            // Liquidation value = 9.8039 * 115 = 1127.45
            // PNL = 127.45
            // Final Capital = 9000 + 1127.45 = 10127.45
            result.FinalCapital.Should().BeApproximately(10127.45m, 2);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.Closed);
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
            // With the new logic, the first BUY signal opens a long position.
            // The second SELL signal closes the long position AND opens a new short position.
            // This short position is then liquidated at the end.
            // --- Trade 1 (Long) ---
            // Initial Capital: 10000, Trade Size: 1000
            // Entry: 102, Exit: 112
            // PNL: (112 - 102) * (1000 / 102) = 98.04
            // Capital after trade 1: 10000 + 98.04 = 10098.04
            // --- Trade 2 (Short) ---
            // Capital: 10098.04, Trade Size: 1009.80
            // Entry: 112
            // Liquidation Price: 120 (last candle close)
            // PNL: (112 - 120) * (1009.80 / 112) = -72.13
            // Final Capital: 10098.04 - 72.13 = 10025.91
            result.FinalCapital.Should().BeApproximately(10025.91m, 2);
            result.Trades.Should().HaveCount(2);
            result.Trades[0].Pnl.Should().BeApproximately(98.04m, 2);
            result.Trades[1].Pnl.Should().BeApproximately(-72.13m, 2);
            result.WinCount.Should().Be(1);
            result.LossCount.Should().Be(1);
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
            // --- Trade 1 (Long) ---
            // Initial Capital: 10000, Trade Size: 1000
            // Entry: 102, Exit: 94
            // PNL: (94 - 102) * (1000 / 102) = -78.43
            // Capital after trade 1: 10000 - 78.43 = 9921.57
            // --- Trade 2 (Short) ---
            // Capital: 9921.57, Trade Size: 992.16
            // Entry: 94
            // Liquidation Price: 90 (last candle close)
            // PNL: (94 - 90) * (992.16 / 94) = 42.22
            // Final Capital: 9921.57 + 42.22 = 9963.79
            result.FinalCapital.Should().BeApproximately(9963.79m, 2);
            result.Trades.Should().HaveCount(2);
            result.Trades[0].Pnl.Should().BeApproximately(-78.43m, 2);
            result.Trades[1].Pnl.Should().BeApproximately(42.22m, 2);
            result.LossCount.Should().Be(1);
            result.WinCount.Should().Be(1);
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

        [Fact]
        public async Task RunAsync_ProfitableShortTrade_ShouldCalculateCorrectPnl()
        {
            var parameters = CreateDefaultParameters();
            var signalTime = parameters.From.AddDays(1);
            var entryTime = parameters.From.AddDays(2);
            var exitSignalTime = parameters.From.AddDays(2);
            var exitTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime, Close = 100 },
                new Candle { Timestamp = entryTime, Open = 98, Close = 95 }, // Entry here (short), signal to buy back
                new Candle { Timestamp = exitTime, Open = 94, Close = 90 }   // Exit here
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Sell, Timestamp = signalTime, Price = 100, Symbol = _symbol },
                new Signal { Type = SignalType.Buy, Timestamp = exitSignalTime, Price = 95, Symbol = _symbol }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Calculation ---
            // --- Trade 1 (Short) ---
            // Initial Capital: 10000, Trade Size: 1000
            // Entry: 98, Exit: 94
            // PNL: (98 - 94) * (1000 / 98) = 40.82
            // Capital after trade 1: 10000 + 40.82 = 10040.82
            // --- Trade 2 (Long) ---
            // Capital: 10040.82, Trade Size: 1004.08
            // Entry: 94
            // Liquidation Price: 90 (last candle close)
            // PNL: (90 - 94) * (1004.08 / 94) = -42.73
            // Final Capital: 10040.82 - 42.73 = 9998.09
            result.FinalCapital.Should().BeApproximately(9998.09m, 2);
            result.Trades.Should().HaveCount(2);
            result.Trades[0].Pnl.Should().BeApproximately(40.82m, 2);
            result.Trades[1].Pnl.Should().BeApproximately(-42.73m, 2);
            result.WinCount.Should().Be(1);
            result.LossCount.Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_LosingShortTrade_ShouldCalculateCorrectPnl()
        {
            var parameters = CreateDefaultParameters();
            var signalTime = parameters.From.AddDays(1);
            var entryTime = parameters.From.AddDays(2);
            var exitSignalTime = parameters.From.AddDays(2);
            var exitTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime, Close = 100 },
                new Candle { Timestamp = entryTime, Open = 102, Close = 105 }, // Entry here (short), signal to buy back
                new Candle { Timestamp = exitTime, Open = 106, Close = 110 }   // Exit here
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Sell, Timestamp = signalTime, Price = 100, Symbol = _symbol },
                new Signal { Type = SignalType.Buy, Timestamp = exitSignalTime, Price = 105, Symbol = _symbol }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Calculation ---
            // --- Trade 1 (Short) ---
            // Initial Capital: 10000, Trade Size: 1000
            // Entry: 102, Exit: 106
            // PNL: (102 - 106) * (1000 / 102) = -39.22
            // Capital after trade 1: 10000 - 39.22 = 9960.78
            // --- Trade 2 (Long) ---
            // Capital: 9960.78, Trade Size: 996.08
            // Entry: 106
            // Liquidation Price: 110 (last candle close)
            // PNL: (110 - 106) * (996.08 / 106) = 37.59
            // Final Capital: 9960.78 + 37.59 = 9998.37
            result.FinalCapital.Should().BeApproximately(9998.37m, 2);
            result.Trades.Should().HaveCount(2);
            result.Trades[0].Pnl.Should().BeApproximately(-39.22m, 2);
            result.Trades[1].Pnl.Should().BeApproximately(37.59m, 2);
            result.LossCount.Should().Be(1);
            result.WinCount.Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_ShortTradeHitsTakeProfit_ShouldCloseAtTpPrice()
        {
            var parameters = CreateDefaultParameters();
            var signalTime = parameters.From.AddDays(1);
            var entryTime = parameters.From.AddDays(2);
            var exitTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime, Close = 100 },
                new Candle { Timestamp = entryTime, Open = 98, High = 99, Low = 97, Close = 97 }, // Entry (short)
                new Candle { Timestamp = exitTime, Open = 96, High = 97, Low = 88, Close = 90 }  // TP hit here
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Sell, Timestamp = signalTime, Price = 100, Symbol = _symbol, TakeProfit = 90 }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Calculation ---
            // Entry Price: 98. TP: 90.
            // PNL = 1000 - (1000/98 * 90) = 1000 - 918.37 = 81.63
            // Final Capital = 10000 + 81.63 = 10081.63
            result.FinalCapital.Should().BeApproximately(10081.63m, 2);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().ExitPrice.Should().Be(90);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.TakeProfit);
        }

        [Fact]
        public async Task RunAsync_ShortTradeHitsStopLoss_ShouldCloseAtSlPrice()
        {
            var parameters = CreateDefaultParameters();
            var signalTime = parameters.From.AddDays(1);
            var entryTime = parameters.From.AddDays(2);
            var exitTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime, Close = 100 },
                new Candle { Timestamp = entryTime, Open = 98, High = 99, Low = 97, Close = 97 }, // Entry (short)
                new Candle { Timestamp = exitTime, Open = 99, High = 112, Low = 98, Close = 110 } // SL hit here
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Sell, Timestamp = signalTime, Price = 100, Symbol = _symbol, StopLoss = 110 }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Calculation ---
            // Entry Price: 98. SL: 110.
            // PNL = 1000 - (1000/98 * 110) = 1000 - 1122.45 = -122.45
            // Final Capital = 10000 - 122.45 = 9877.55
            result.FinalCapital.Should().BeApproximately(9877.55m, 2);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().ExitPrice.Should().Be(110);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.StopLoss);
        }

        [Fact]
        public async Task RunAsync_MultipleOpenPositions_ShouldCloseOldestFirst_FIFO()
        {
            // Arrange
            var parameters = CreateDefaultParameters();
            var signalTime1 = parameters.From.AddDays(1);
            var entryTime1 = parameters.From.AddDays(2);
            var signalTime2 = parameters.From.AddDays(2);
            var entryTime2 = parameters.From.AddDays(3);
            var closeSignalTime = parameters.From.AddDays(3);
            var exitTime = parameters.From.AddDays(4);
            var endTime = parameters.From.AddDays(5);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = signalTime1, Close = 100 },      // Signal 1 (Buy)
                new Candle { Timestamp = entryTime1, Open = 102 },        // Entry 1
                new Candle { Timestamp = entryTime2, Open = 104 },        // Entry 2
                new Candle { Timestamp = exitTime, Open = 108 },          // Exit 1, Entry 3 (Short)
                new Candle { Timestamp = endTime, Close = 110 }           // Liquidation of 2nd and 3rd positions
            };

            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = signalTime1, Symbol = _symbol },
                new Signal { Type = SignalType.Buy, Timestamp = signalTime2, Symbol = _symbol },
                new Signal { Type = SignalType.Sell, Timestamp = closeSignalTime, Symbol = _symbol }
            };

            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            // Act
            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // Assert
            result.Trades.Should().HaveCount(3);

            var firstTrade = result.Trades.FirstOrDefault(t => t.EntryPrice == 102);
            var secondTrade = result.Trades.FirstOrDefault(t => t.EntryPrice == 104);
            var thirdTrade = result.Trades.FirstOrDefault(t => t.EntryPrice == 108); // This is the reversal trade

            firstTrade.Should().NotBeNull();
            firstTrade.ExitPrice.Should().Be(108); // Closed by the Sell signal
            firstTrade.Outcome.Should().Be(TradeOutcome.Closed);

            secondTrade.Should().NotBeNull();
            secondTrade.ExitPrice.Should().Be(110); // Liquidated at the end
            secondTrade.Outcome.Should().Be(TradeOutcome.Closed);

            thirdTrade.Should().NotBeNull();
            thirdTrade.Side.Should().Be(OrderSide.Sell);
            thirdTrade.ExitPrice.Should().Be(110); // Liquidated at the end
            thirdTrade.Outcome.Should().Be(TradeOutcome.Closed);
        }
    }
}
