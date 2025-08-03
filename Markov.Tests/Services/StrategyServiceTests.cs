using Xunit;
using Moq;
using FluentAssertions;
using Markov.API.Models;
using Markov.API.Services;
using Markov.Services;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Markov.Services.Enums;

namespace Markov.Tests.Services
{
    public class StrategyServiceTests
    {
        private readonly Mock<DbSet<StrategyConfiguration>> _mockSet;
        private readonly Mock<MarkovDbContext> _mockContext;
        private readonly StrategyService _strategyService;

        public StrategyServiceTests()
        {
            var data = new List<StrategyConfiguration>().AsQueryable();

            _mockSet = new Mock<DbSet<StrategyConfiguration>>();
            _mockSet.As<IQueryable<StrategyConfiguration>>().Setup(m => m.Provider).Returns(data.Provider);
            _mockSet.As<IQueryable<StrategyConfiguration>>().Setup(m => m.Expression).Returns(data.Expression);
            _mockSet.As<IQueryable<StrategyConfiguration>>().Setup(m => m.ElementType).Returns(data.ElementType);
            _mockSet.As<IQueryable<StrategyConfiguration>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            _mockContext = new Mock<MarkovDbContext>(new DbContextOptions<MarkovDbContext>());
            _mockContext.Setup(m => m.StrategyConfigurations).Returns(_mockSet.Object);
            _strategyService = new StrategyService(_mockContext.Object);
        }

        [Fact]
        public void CreateStrategy_WithValidRequest_ShouldSaveToDatabase()
        {
            var request = new CreateStrategyRequest
            {
                StrategyName = "Simple MACD Strategy",
                Filters = new List<FilterDto> { new FilterDto { Name = "TrendFilter" } }
            };

            var strategyId = _strategyService.CreateStrategy(request);

            strategyId.Should().NotBeEmpty();
            _mockSet.Verify(m => m.Add(It.IsAny<StrategyConfiguration>()), Times.Once());
            _mockContext.Verify(m => m.SaveChanges(), Times.Once());
        }

        [Fact]
        public void GetStrategy_WithExistingId_ShouldReconstructAndApplyFilter()
        {
            var strategyId = Guid.NewGuid();
            var filters = new List<FilterDto>
            {
                new FilterDto
                {
                    Name = "TrendFilter",
                    Parameters = new Dictionary<string, object> { { "longTermMAPeriod", 2 } }
                }
            };
            var config = new StrategyConfiguration
            {
                Id = strategyId,
                StrategyName = "Simple MACD Strategy",
                FiltersJson = JsonSerializer.Serialize(filters)
            };
            
            _mockSet.Setup(m => m.Find(strategyId)).Returns(config);
            
            var strategy = _strategyService.GetStrategy(strategyId);
            strategy.Should().NotBeNull();
            
            var signalTime = DateTime.UtcNow;
            var candles = new List<Candle>
            {
                new Candle { Close = 100, Timestamp = signalTime.AddDays(-1) },
                new Candle { Close = 90, Timestamp = signalTime } 
            };
            var signals = new List<Signal> { new Signal { Type = SignalType.Buy, Symbol = "BTC", Timestamp = signalTime } };
            var data = new Dictionary<string, IEnumerable<Candle>> { { "BTC", candles } };
            
            var filteredSignals = strategy.GetFilteredSignals(data);
            
            filteredSignals.Should().BeEmpty();
        }

        [Fact]
        public void GetStrategy_WithNonExistingId_ShouldThrowKeyNotFoundException()
        {
            var nonExistentId = Guid.NewGuid();
            _mockSet.Setup(m => m.Find(nonExistentId)).Returns((StrategyConfiguration)null);

            Action act = () => _strategyService.GetStrategy(nonExistentId);

            act.Should().Throw<KeyNotFoundException>();
        }
    }
}