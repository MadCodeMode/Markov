using Xunit;
using FluentAssertions;
using Markov.Services.Models;
using Markov.Services.Filters;
using System.Collections.Generic;
using System.Linq;
using Markov.Services.Enums;

namespace Markov.Tests.Filters
{
    public class EmergencyStopFilterTests
    {
        [Fact]
        public void Apply_WithAnySignals_ShouldReturnEmptyCollection()
        {
            var signals = new List<Signal>
            {
                new Signal { Type = SignalType.Buy },
                new Signal { Type = SignalType.Sell }
            };
            var filter = new EmergencyStopFilter();

            var result = filter.Apply(signals, new Dictionary<string, IEnumerable<Candle>>());

            result.Should().BeEmpty();
        }

        [Fact]
        public void Apply_WithEmptySignals_ShouldReturnEmptyCollection()
        {
            var signals = Enumerable.Empty<Signal>();
            var filter = new EmergencyStopFilter();

            var result = filter.Apply(signals, new Dictionary<string, IEnumerable<Candle>>());

            result.Should().BeEmpty();
        }

        [Fact]
        public void Name_ShouldReturnCorrectName()
        {
            var filter = new EmergencyStopFilter();
            
            var name = filter.Name;
            
            name.Should().Be("EmergencyStop");
        }
    }
}