using Xunit;
using Moq;
using FluentAssertions;
using Markov.Services.Models;
using Markov.Services.Filters;
using Markov.Services.Interfaces;
using System.Collections.Generic;
using System;
using System.Linq;
using Markov.Services.Enums;

namespace Markov.Tests.Filters
{
    public class TrendFilterTests
    {
        private readonly Mock<IIndicator> _mockSmaIndicator;
        private readonly TrendFilter _trendFilter;
        private readonly string _symbol = "BTCUSDT";

        public TrendFilterTests()
        {
            _mockSmaIndicator = new Mock<IIndicator>();
            _trendFilter = new TrendFilter(_mockSmaIndicator.Object);
        }

        [Fact]
        public void Apply_BuySignalInUptrend_ShouldAllowSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime, Close = 110 } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Buy, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };

            _mockSmaIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 100 });
            
            var result = _trendFilter.Apply(signals, data);
            
            result.Should().HaveCount(1);
        }

        [Fact]
        public void Apply_BuySignalInDowntrend_ShouldFilterSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime, Close = 90 } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Buy, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };

            _mockSmaIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 100 });
            
            var result = _trendFilter.Apply(signals, data);
            
            result.Should().BeEmpty();
        }

        [Fact]
        public void Apply_SellSignalInDowntrend_ShouldAllowSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime, Close = 90 } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Sell, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };

            _mockSmaIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 100 });
            
            var result = _trendFilter.Apply(signals, data);
            
            result.Should().HaveCount(1);
        }

        [Fact]
        public void Apply_SellSignalInUptrend_ShouldFilterSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime, Close = 110 } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Sell, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };

            _mockSmaIndicator.Setup(i => i.Calculate(candles)).Returns(new List<decimal> { 100 });
            
            var result = _trendFilter.Apply(signals, data);
            
            result.Should().BeEmpty();
        }
    }
}