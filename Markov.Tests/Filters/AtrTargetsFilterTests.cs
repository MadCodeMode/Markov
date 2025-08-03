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
    public class AtrTargetsFilterTests
    {
        private readonly Mock<IIndicator> _mockAtrIndicator;
        private readonly AtrTargetsFilter _filter;
        private readonly string _symbol = "BTCUSDT";

        public AtrTargetsFilterTests()
        {
            _mockAtrIndicator = new Mock<IIndicator>();
            _filter = new AtrTargetsFilter(_mockAtrIndicator.Object);
        }

        [Fact]
        public void Apply_BuySignal_ShouldSetCorrectAtrTargets()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Buy, Price = 100, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };

            _mockAtrIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 10 });
            
            var result = _filter.Apply(signals, data).First();

            result.TakeProfit.Should().Be(120); // 100 + (10 * 2.0)
            result.StopLoss.Should().Be(85);   // 100 - (10 * 1.5)
        }

        [Fact]
        public void Apply_SellSignal_ShouldSetCorrectAtrTargets()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Sell, Price = 100, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };
            
            _mockAtrIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 10 });

            var result = _filter.Apply(signals, data).First();

            result.TakeProfit.Should().Be(80);  // 100 - (10 * 2.0)
            result.StopLoss.Should().Be(115); // 100 + (10 * 1.5)
        }

        [Fact]
        public void Apply_ZeroAtrValue_ShouldNotSetTargets()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Buy, Price = 100, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };

            _mockAtrIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 0 });

            var result = _filter.Apply(signals, data).First();

            result.TakeProfit.Should().BeNull();
            result.StopLoss.Should().BeNull();
        }
    }
}