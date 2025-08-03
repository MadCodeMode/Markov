using Xunit;
using FluentAssertions;
using Markov.Services.Models;
using Markov.Services.Indicators;
using System.Collections.Generic;
using System.Linq;

namespace Markov.Tests.Indicators
{
    public class RsiIndicatorTests
    {
        [Fact]
        public void Calculate_WithSufficientData_ShouldReturnCorrectRsiValues()
        {
            var candles = new List<Candle>
            {
                new Candle { Close = 44.34m }, new Candle { Close = 44.09m },
                new Candle { Close = 44.15m }, new Candle { Close = 43.61m },
                new Candle { Close = 44.33m }, new Candle { Close = 44.83m },
                new Candle { Close = 45.10m }, new Candle { Close = 45.42m },
                new Candle { Close = 45.84m }, new Candle { Close = 46.08m },
                new Candle { Close = 45.89m }, new Candle { Close = 46.03m },
                new Candle { Close = 45.61m }, new Candle { Close = 46.28m },
                new Candle { Close = 46.28m } // 15 candles total, period 14
            };
            var rsiIndicator = new RsiIndicator(14);
            
            var result = rsiIndicator.Calculate(candles).ToList();

            result.Should().HaveCount(1);
            result.First().Should().BeApproximately(70.46m, 0.01m);
        }

        [Fact]
        public void Calculate_WithNoLosses_ShouldReturn100()
        {
            var candles = Enumerable.Range(1, 15).Select(i => new Candle { Close = 10 + i }).ToList();
            var rsiIndicator = new RsiIndicator(14);
            
            var result = rsiIndicator.Calculate(candles).ToList();

            result.First().Should().Be(100);
        }
    }
}