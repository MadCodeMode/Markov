using Xunit;
using FluentAssertions;
using Markov.Services.Models;
using Markov.Services.Filters;
using System.Collections.Generic;
using System.Linq;
using Markov.Trading.Engine.Filters;
using Markov.Services.Enums;

namespace Markov.Tests.Filters
{
    public class TakeProfitStopLossFilterTests
    {
        [Fact]
        public void Apply_BuySignal_ShouldSetCorrectTpAndSl()
        {
            var signals = new List<Signal> { new Signal { Type = SignalType.Buy, Price = 100 } };
            var filter = new TakeProfitStopLossFilter(0.10m, 0.05m);

            var result = filter.Apply(signals, new Dictionary<string, IEnumerable<Candle>>()).First();

            result.TakeProfit.Should().Be(110);
            result.StopLoss.Should().Be(95);
            result.UseHoldStrategy.Should().BeFalse();
        }

        [Fact]
        public void Apply_SellSignal_ShouldSetCorrectTpAndSl()
        {
            var signals = new List<Signal> { new Signal { Type = SignalType.Sell, Price = 100 } };
            var filter = new TakeProfitStopLossFilter(0.10m, 0.05m);

            var result = filter.Apply(signals, new Dictionary<string, IEnumerable<Candle>>()).First();

            result.TakeProfit.Should().Be(90);
            result.StopLoss.Should().Be(105);
            result.UseHoldStrategy.Should().BeFalse();
        }

        [Fact]
        public void Apply_WithHoldStrategyForLongs_ShouldSetHoldAndNullifySl()
        {
            var signals = new List<Signal> { new Signal { Type = SignalType.Buy, Price = 100 } };
            var filter = new TakeProfitStopLossFilter(0.10m, 0.05m, useHoldStrategyForLongs: true);
            
            var result = filter.Apply(signals, new Dictionary<string, IEnumerable<Candle>>()).First();

            result.TakeProfit.Should().Be(110);
            result.StopLoss.Should().BeNull();
            result.UseHoldStrategy.Should().BeTrue();
        }

        [Fact]
        public void Apply_SignalWithExistingTp_ShouldNotOverride()
        {
            var signals = new List<Signal> { new Signal { Type = SignalType.Buy, Price = 100, TakeProfit = 150, StopLoss = 50 } };
            var filter = new TakeProfitStopLossFilter(0.10m, 0.05m);

            var result = filter.Apply(signals, new Dictionary<string, IEnumerable<Candle>>()).First();

            result.TakeProfit.Should().Be(150);
            result.StopLoss.Should().Be(50);
        }
    }
}