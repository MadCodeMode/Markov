using Xunit;
using FluentAssertions;
using Markov.Services.Models;
using Markov.Services.Indicators;
using System.Collections.Generic;
using System.Linq;

namespace Markov.Tests.Indicators
{
    public class SmaIndicatorTests
    {
        [Fact]
        public void Calculate_WithSufficientData_ShouldReturnCorrectSmaValues()
        {
            var candles = new List<Candle>
            {
                new Candle { Close = 10 },
                new Candle { Close = 12 },
                new Candle { Close = 15 },
                new Candle { Close = 11 },
                new Candle { Close = 14 }
            };
            var smaIndicator = new SmaIndicator(3);

            var result = smaIndicator.Calculate(candles).ToList();
            
            result.Should().HaveCount(5);
            result[0].Should().Be(0);
            result[1].Should().Be(0);
            result[2].Should().BeApproximately(12.33m, 0.01m); // (10+12+15)/3
            result[3].Should().BeApproximately(12.67m, 0.01m); // (12+15+11)/3
            result[4].Should().BeApproximately(13.33m, 0.01m); // (15+11+14)/3
        }

        [Fact]
        public void Calculate_WithInsufficientData_ShouldReturnZeros()
        {
            var candles = new List<Candle>
            {
                new Candle { Close = 10 },
                new Candle { Close = 12 }
            };
            var smaIndicator = new SmaIndicator(3);
            
            var result = smaIndicator.Calculate(candles).ToList();
            
            result.Should().HaveCount(2);
            result.Should().OnlyContain(v => v == 0);
        }

        [Fact]
        public void Calculate_WithEmptyData_ShouldReturnEmptyCollection()
        {
            var candles = new List<Candle>();
            var smaIndicator = new SmaIndicator(5);
            
            var result = smaIndicator.Calculate(candles);
            
            result.Should().BeEmpty();
        }

        [Fact]
        public void Name_ShouldReturnCorrectlyFormattedName()
        {
            var smaIndicator = new SmaIndicator(20);
            
            var name = smaIndicator.Name;
            
            name.Should().Be("SMA(20)");
        }
    }
}