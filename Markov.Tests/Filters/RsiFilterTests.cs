using Xunit;
using Moq;
using FluentAssertions;
using Markov.Services.Models;
using Markov.Services.Filters;
using Markov.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Markov.Services.Enums;

namespace Markov.Tests.Filters
{
    public class RsiFilterTests
    {
        private readonly Mock<IIndicator> _mockRsiIndicator;
        private readonly RsiFilter _filter;
        private readonly string _symbol = "BTCUSDT";

        public RsiFilterTests()
        {
            _mockRsiIndicator = new Mock<IIndicator>();
            _filter = new RsiFilter(_mockRsiIndicator.Object);
        }

        [Fact]
        public void Apply_BuySignalWhenNotOversold_ShouldFilterSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Buy, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };

            _mockRsiIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 40 }); // RSI is above oversold threshold (30)

            var result = _filter.Apply(signals, data);

            result.Should().BeEmpty();
        }

        [Fact]
        public void Apply_BuySignalWhenOversold_ShouldAllowSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Buy, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };

            _mockRsiIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 20 }); // RSI is below oversold threshold (30)

            var result = _filter.Apply(signals, data);

            result.Should().HaveCount(1);
        }

        [Fact]
        public void Apply_SellSignalWhenNotOverbought_ShouldFilterSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Sell, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };
            
            _mockRsiIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 60 }); // RSI is below overbought threshold (70)

            var result = _filter.Apply(signals, data);
            
            result.Should().BeEmpty();
        }

        [Fact]
        public void Apply_SellSignalWhenOverbought_ShouldAllowSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Sell, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };

            _mockRsiIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 80 }); // RSI is above overbought threshold (70)

            var result = _filter.Apply(signals, data);

            result.Should().HaveCount(1);
        }
    }
}