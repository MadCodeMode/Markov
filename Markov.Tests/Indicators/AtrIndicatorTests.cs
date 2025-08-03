using Xunit;
using FluentAssertions;
using Markov.Services.Models;
using Markov.Services.Indicators;
using System.Collections.Generic;
using System.Linq;

namespace Markov.Tests.Indicators
{
    public class AtrIndicatorTests
    {
        [Fact]
        public void Calculate_WithSufficientData_ShouldReturnCorrectAtrValues()
        {
            var candles = new List<Candle>
            {
                new Candle { High = 10, Low = 8, Close = 9 },
                new Candle { High = 11, Low = 9, Close = 10 },
                new Candle { High = 12, Low = 10, Close = 11 }
            };
            var atrIndicator = new AtrIndicator(2);
            
            var result = atrIndicator.Calculate(candles).ToList();
            
            result.Should().HaveCount(3);
            result[0].Should().Be(0); 
            result[1].Should().BeApproximately(2.0m, 0.01m); // TR1=2, TR2=2. Avg = 2
            result[2].Should().BeApproximately(2.0m, 0.01m); // (2*1 + 2)/2
        }

        [Fact]
        public void Calculate_WithInsufficientData_ShouldReturnZeros()
        {
            var candles = new List<Candle>
            {
                new Candle { High = 10, Low = 8, Close = 9 }
            };
            var atrIndicator = new AtrIndicator(2);
            
            var result = atrIndicator.Calculate(candles).ToList();
            
            result.Should().HaveCount(1);
            result.First().Should().Be(0);
        }

        [Fact]
        public void Name_ShouldReturnCorrectlyFormattedName()
        {
            var atrIndicator = new AtrIndicator(14);
            
            var name = atrIndicator.Name;
            
            name.Should().Be("ATR(14)");
        }
    }
}