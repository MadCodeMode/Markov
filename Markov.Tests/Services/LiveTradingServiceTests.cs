using Moq;
using FluentAssertions;
using Markov.Services;
using Markov.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Markov.API.Services;
using LiveSession = Markov.Services.Models.LiveSession;
using Markov.Services.Time;
using Castle.Core.Logging;
using Markov.Services.Engine;
using Microsoft.Extensions.Logging;

namespace Markov.Tests.Services;

public class LiveTradingServiceTests
{
    private readonly Mock<IStrategyService> _mockStrategyService;
    private readonly Mock<IExchange> _mockExchange;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<DbSet<LiveSession>> _mockSessionSet;
    private readonly Mock<MarkovDbContext> _mockDbContext;
    private readonly LiveTradingService _liveTradingService;
    private readonly Mock<ITimerService> _mockTimerService;
    private readonly Mock<ILogger<TradingEngine>> _logger;


    public LiveTradingServiceTests()
    {
        _mockStrategyService = new Mock<IStrategyService>();
        _mockExchange = new Mock<IExchange>();
        _mockTimerService = new Mock<ITimerService>();
        _logger = new Mock<ILogger<TradingEngine>>();

        var sessions = new List<LiveSession>();
        _mockSessionSet = new Mock<DbSet<LiveSession>>();
        _mockSessionSet.As<IQueryable<LiveSession>>().Setup(m => m.Provider).Returns(sessions.AsQueryable().Provider);
        _mockSessionSet.As<IQueryable<LiveSession>>().Setup(m => m.Expression).Returns(sessions.AsQueryable().Expression);
        _mockSessionSet.As<IQueryable<LiveSession>>().Setup(m => m.ElementType).Returns(sessions.AsQueryable().ElementType);
        _mockSessionSet.As<IQueryable<LiveSession>>().Setup(m => m.GetEnumerator()).Returns(sessions.GetEnumerator());

        _mockDbContext = new Mock<MarkovDbContext>(new DbContextOptions<MarkovDbContext>());
        _mockDbContext.Setup(c => c.LiveSessions).Returns(_mockSessionSet.Object);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(p => p.GetService(typeof(MarkovDbContext))).Returns(_mockDbContext.Object);

        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        _liveTradingService = new LiveTradingService(_mockStrategyService.Object, _mockExchange.Object, _mockScopeFactory.Object, _mockTimerService.Object, _logger.Object);
    }

    [Fact]
    public void StartSession_WithValidStrategy_ShouldCreateAndStoreSession()
    {
        var strategyId = Guid.NewGuid();
        var mockStrategy = new Mock<IStrategy>();
        _mockStrategyService.Setup(s => s.GetStrategy(strategyId)).Returns(mockStrategy.Object);

        var sessionId = _liveTradingService.StartSession(strategyId, "BTCUSDT", "OneDay");

        sessionId.Should().NotBeEmpty();
        _mockSessionSet.Verify(m => m.Add(It.Is<LiveSession>(s => s.Id == sessionId && s.Status == "Running")), Times.Once());
        _mockDbContext.Verify(m => m.SaveChanges(), Times.Once());
    }

    [Fact]
    public void StartSession_WithInvalidStrategy_ShouldThrowException()
    {
        var invalidStrategyId = Guid.NewGuid();
        _mockStrategyService.Setup(s => s.GetStrategy(invalidStrategyId)).Throws<KeyNotFoundException>();

        Action act = () => _liveTradingService.StartSession(invalidStrategyId, "BTCUSDT", "OneDay");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void StopSession_WithRunningSession_ShouldUpdateStatusInDb()
    {
        var sessionId = Guid.NewGuid();
        var session = new LiveSession { Id = sessionId, Status = "Running" };
        // --- THE FIX: Mock the Find method directly ---
        _mockSessionSet.Setup(m => m.Find(sessionId)).Returns(session);

        _liveTradingService.StopSession(sessionId);

        session.Status.Should().Be("Stopped");
        _mockDbContext.Verify(m => m.SaveChanges(), Times.Once());
    }

    [Fact]
    public void StopSession_WithNonExistentSession_ShouldThrowKeyNotFoundException()
    {
        var nonExistentId = Guid.NewGuid();
        _mockSessionSet.Setup(m => m.Find(nonExistentId)).Returns((LiveSession?)null);

        Action act = () => _liveTradingService.StopSession(nonExistentId);

        act.Should().Throw<KeyNotFoundException>().WithMessage("Live session not found in database.");
    }

    [Fact]
    public void GetSession_WithExistingId_ShouldReturnCorrectDto()
    {
        var sessionId = Guid.NewGuid();
        var strategyId = Guid.NewGuid();
        var session = new LiveSession { Id = sessionId, StrategyId = strategyId, Symbol = "ETHUSDT", Status = "Running" };
        var mockStrategy = new Mock<IStrategy>();
        mockStrategy.Setup(s => s.Name).Returns("Test Strategy");
        _mockSessionSet.Setup(m => m.Find(sessionId)).Returns(session);
        _mockStrategyService.Setup(s => s.GetStrategy(strategyId)).Returns(mockStrategy.Object);

        var dto = _liveTradingService.GetSession(sessionId);

        dto.Should().NotBeNull();
        dto.SessionId.Should().Be(sessionId);
        dto.Symbol.Should().Be("ETHUSDT");
        dto.StrategyName.Should().Be("Test Strategy");
    }
}