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
    public class VolumeFilterTests
    {
        private readonly Mock<IIndicator> _mockVolumeMaIndicator;
        private readonly VolumeFilter _filter;
        private readonly string _symbol = "BTCUSDT";

        public VolumeFilterTests()
        {
            _mockVolumeMaIndicator = new Mock<IIndicator>();
            _filter = new VolumeFilter(_mockVolumeMaIndicator.Object);
        }

        [Fact]
        public void Apply_SignalWithHighVolume_ShouldAllowSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime, Volume = 160 } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Buy, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };
            
            _mockVolumeMaIndicator.Setup(i => i.Calculate(It.IsAny<IEnumerable<Candle>>())).Returns(new List<decimal> { 100 });

            var result = _filter.Apply(signals, data);

            result.Should().HaveCount(1);
        }

        [Fact]
        public void Apply_SignalWithLowVolume_ShouldFilterSignal()
        {
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle> { new Candle { Timestamp = signalTime, Volume = 140 } };
            var signals = new List<Signal> { new Signal { Symbol = _symbol, Type = SignalType.Buy, Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { _symbol, candles } };
            
            _mockVolumeMaIndicator.Setup(i => i.Calculate(It.IsAny<IEnumerable<Candle>>())).Returns(new List<decimal> { 100 });

            var result = _filter.Apply(signals, data);
            
            result.Should().BeEmpty();
        }
    }
}