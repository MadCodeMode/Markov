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
                SlippagePercentage = slippage
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
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // Initial: 10000. Trade size: 1000. PNL: +100. Capital returned: 1100.
            // Final capital: 9000 (remaining) + 1100 = 10100
            result.FinalCapital.Should().Be(10100);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().Pnl.Should().Be(100);
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
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // Initial: 10000. Trade size: 1000. PNL: -100. Capital returned: 900.
            // Final capital: 9000 (remaining) + 900 = 9900
            result.FinalCapital.Should().Be(9900);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().Pnl.Should().Be(-100);
            result.LossCount.Should().Be(1);
        }

        [Fact]
        public async Task RunAsync_TakeProfitHit_ShouldCloseAtTpPrice()
        {
            var parameters = CreateDefaultParameters();
            var entryTime = parameters.From.AddDays(1);
            var exitTime = parameters.From.AddDays(2);
            var candles = new List<Candle>
            {
                new Candle { Timestamp = entryTime, Close = 100 },
                new Candle { Timestamp = exitTime, Close = 115, High = 120, Low = 95 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = entryTime, Price = 100, Symbol = _symbol, TakeProfit = 110 }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.FinalCapital.Should().Be(10100);
            result.Trades.First().ExitPrice.Should().Be(110);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.TakeProfit);
        }

        [Fact]
        public async Task RunAsync_StopLossHit_ShouldCloseAtSlPrice()
        {
            var parameters = CreateDefaultParameters();
            var entryTime = parameters.From.AddDays(1);
            var exitTime = parameters.From.AddDays(2);
            var candles = new List<Candle>
            {
                new Candle { Timestamp = entryTime, Close = 100 },
                new Candle { Timestamp = exitTime, Close = 95, High = 105, Low = 85 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = entryTime, Price = 100, Symbol = _symbol, StopLoss = 90 }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.FinalCapital.Should().Be(9900);
            result.Trades.First().ExitPrice.Should().Be(90);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.StopLoss);
        }

        [Fact]
        public async Task RunAsync_HoldStrategy_ShouldOnlyUseTradeCapitalAndAllowFurtherTrades()
        {
            var parameters = CreateDefaultParameters(10000);
            var holdEntryTime = parameters.From.AddDays(1);
            var tradeEntryTime = parameters.From.AddDays(2);
            var tradeExitTime = parameters.From.AddDays(3);

            var candles = new List<Candle>
            {
                new Candle { Timestamp = holdEntryTime, Close = 100 },
                new Candle { Timestamp = tradeEntryTime, Close = 110 },
                new Candle { Timestamp = tradeExitTime, Close = 120 },
                new Candle { Timestamp = parameters.To.AddDays(-1), Close = 150 } // Final price for held asset
            };
            var signals = new List<Signal>
            {
                // 1. First signal is a hold
                new Signal { Type = SignalType.Buy, Timestamp = holdEntryTime, Price = 100, Symbol = _symbol, UseHoldStrategy = true },
                // 2. Second is a regular trade
                new Signal { Type = SignalType.Buy, Timestamp = tradeEntryTime, Price = 110, Symbol = "ETHUSDT" },
                new Signal { Type = SignalType.Sell, Timestamp = tradeExitTime, Price = 120, Symbol = "ETHUSDT" }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Assertions for the Hold Trade ---
            // 1. Initial capital is 10000. First trade is a hold, using 10% (1000).
            //    Remaining capital becomes 9000.
            //    Quantity of held asset is 1000 / 100 = 10.
            result.HeldAssets[_symbol].Should().BeApproximately(10m, 0.001m);
            result.HoldCount.Should().Be(1);

            // --- Assertions for the Second Trade ---
            // 2. Second trade opens with 10% of remaining capital (9000 * 0.1 = 900).
            //    Remaining capital becomes 8100.
            //    PNL of second trade is profitable: (120/110 * 900) - 900 = ~81.81
            //    Capital returned from trade: 900 + 81.81 = 981.81
            result.Trades.Should().HaveCount(1);
            result.Trades.First().Pnl.Should().BeApproximately(81.81m, 0.01m);

            // --- Final Assertions ---
            // 3. Final capital = 8100 (remaining) + 981.81 (from trade) = 9081.81
            result.FinalCapital.Should().BeApproximately(9081.81m, 0.01m);
            // 4. Value of held assets at the end is 10 * 150 = 1500
            result.FinalHeldAssetsValue.Should().Be(1500);
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
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // Initial: 10000. Trade size: 1000. Position value at end: 1000 * 1.05 = 1050.
            // Final capital: 9000 (remaining) + 1050 = 10050
            result.FinalCapital.Should().Be(10050);
            result.Trades.Should().BeEmpty();
        }

        [Fact]
        public async Task RunAsync_FixedAmountTrade_ShouldUseCorrectTradeSize()
        {
            var parameters = CreateDefaultParameters();
            parameters.TradeSizeMode = TradeSizeMode.FixedAmount;
            parameters.TradeSizeValue = 500; // Trade exactly 500

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
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // Initial: 10000. Trade size: 500. PNL: 50. Capital returned: 550.
            // Final capital: 9500 (remaining) + 550 = 10050
            result.FinalCapital.Should().Be(10050);
            result.Trades.Should().HaveCount(1);
            result.Trades.First().AmountInvested.Should().Be(500);
        }

        [Fact]
        public async Task RunAsync_WithSlippageAndCommission_ShouldCalculateCorrectFinalCapital()
        {
            var parameters = CreateDefaultParameters();
            parameters.CommissionPercentage = 0.001m; // 0.1%
            parameters.SlippagePercentage = 0.0005m; // 0.05%

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
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // --- Detailed Calculation ---
            // Initial Capital: 10000
            // Trade Size: 10000 * 0.1 = 1000
            //
            // Entry:
            // Slippage on Buy: 100 * (1 + 0.0005) = 100.05
            // Commission on Entry: 1000 * 0.001 = 1
            // Capital after Entry: 10000 - 1000 - 1 = 8999
            // Quantity: (1000 - 1) / 100.05 = 9.995
            //
            // Exit:
            // Slippage on Sell: 110 * (1 - 0.0005) = 109.945
            // Exit Value: 9.995 * 109.945 = 1098.90
            // Commission on Exit: 1098.90 * 0.001 = 1.0989
            //
            // Final Capital: 8999 + 1098.90 - 1.0989 = 10096.8011
            result.FinalCapital.Should().BeApproximately(10096.80m, 0.01m);
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
        public async Task RunAsync_SignalOnLastCandle_ShouldLiquidateAtClose()
        {
            var parameters = CreateDefaultParameters();
            var lastCandleTime = parameters.To.Date;
            var candles = new List<Candle>
            {
                new Candle { Timestamp = lastCandleTime.AddDays(-1), Close = 100 },
                new Candle { Timestamp = lastCandleTime, Close = 105 }
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = lastCandleTime, Price = 105, Symbol = _symbol }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            // Initial: 10000. Trade size: 1000. Liquidated at 105.
            // Final capital: 9000 (remaining) + 1000 (liquidated) = 10000
            result.FinalCapital.Should().Be(10000);
            result.Trades.Should().BeEmpty(); // No closed trades
        }

        [Fact]
        public async Task RunAsync_TpAndSlHitOnSameCandle_ShouldPrioritizeStopLoss()
        {
            var parameters = CreateDefaultParameters();
            var entryTime = parameters.From.AddDays(1);
            var exitTime = parameters.From.AddDays(2);
            var candles = new List<Candle>
            {
                new Candle { Timestamp = entryTime, Close = 100 },
                new Candle { Timestamp = exitTime, Close = 100, High = 110, Low = 90 } // High hits TP, Low hits SL
            };
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy, Timestamp = entryTime, Price = 100, Symbol = _symbol, TakeProfit = 110, StopLoss = 90 }
            };
            _mockExchange.Setup(e => e.GetHistoricalDataAsync(It.IsAny<string>(), It.IsAny<TimeFrame>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(candles);
            _mockStrategy.Setup(s => s.GetFilteredSignals(It.IsAny<IDictionary<string, IEnumerable<Candle>>>())).Returns(signals);

            var result = await _backtestingEngine.RunAsync(_mockStrategy.Object, parameters);

            result.Trades.Should().HaveCount(1);
            result.Trades.First().Outcome.Should().Be(TradeOutcome.StopLoss);
            result.Trades.First().ExitPrice.Should().Be(90);
        }
    }
}