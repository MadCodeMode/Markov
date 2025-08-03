using Markov.API.Models;
using Markov.Services;
using Markov.Services.Engine;
using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using Markov.Services.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Markov.API.Services;

public class LiveTradingService : ILiveTradingService
{
    private static readonly ConcurrentDictionary<Guid, ITradingEngine> _runningEngines = new();
    private readonly IStrategyService _strategyService;
    private readonly IExchange _exchange;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITimerService _timerService;
    private readonly ILogger<TradingEngine> _logger;
    private readonly TradingSettings _tradingSettings;

    public LiveTradingService(
        IStrategyService strategyService,
        IExchange exchange,
        IServiceScopeFactory scopeFactory,
        ITimerService timerService,
        ILogger<TradingEngine> logger,
        IOptions<TradingSettings> tradingSettings)
    {
        _strategyService = strategyService;
        _exchange = exchange;
        _scopeFactory = scopeFactory;
        _timerService = timerService;
        _logger = logger;
        _tradingSettings = tradingSettings.Value;
    }

    public Guid StartSession(Guid strategyId, string symbol, string timeFrame)
    {
        var strategy = _strategyService.GetStrategy(strategyId);
        var sessionId = Guid.NewGuid();
        var symbols = new List<string> { symbol };
        var tf = Enum.Parse<TimeFrame>(timeFrame, true);

        var engine = new TradingEngine(
            _exchange, 
            strategy, 
            symbols, 
            tf, 
            _timerService, 
            _logger, 
            TimeSpan.FromSeconds(_tradingSettings.TradingLoopIntervalSeconds)
        );

        var session = new Markov.Services.Models.LiveSession
        {
            Id = sessionId,
            StrategyId = strategyId,
            Symbol = symbol,
            TimeFrame = timeFrame,
            Status = "Running",
            StartTime = DateTime.UtcNow
        };

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MarkovDbContext>();
            context.LiveSessions.Add(session);
            context.SaveChanges();
        }

        _runningEngines[sessionId] = engine;
        engine.StartAsync();

        return sessionId;
    }

    public void StopSession(Guid sessionId)
    {
        if (_runningEngines.TryRemove(sessionId, out var engine))
        {
            engine.StopAsync();
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MarkovDbContext>();
            var session = context.LiveSessions.Find(sessionId);
            if (session != null)
            {
                session.Status = "Stopped";
                context.SaveChanges();
            }
            else
            {
                throw new KeyNotFoundException("Live session not found in database.");
            }
        }
    }

    public LiveSessionDto GetSession(Guid sessionId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MarkovDbContext>();
            var session = context.LiveSessions.Find(sessionId);

            if (session == null)
            {
                throw new KeyNotFoundException("Live session not found.");
            }

            var strategy = _strategyService.GetStrategy(session.StrategyId);
            return new LiveSessionDto
            {
                SessionId = session.Id,
                StrategyId = session.StrategyId,
                Symbol = session.Symbol,
                Status = session.Status,
                StartTime = session.StartTime,
                StrategyName = strategy.Name
            };
        }
    }

    public IEnumerable<LiveSessionDto> GetAllSessions()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MarkovDbContext>();
            var sessions = context.LiveSessions.ToList();
            return sessions.Select(session =>
            {
                var strategy = _strategyService.GetStrategy(session.StrategyId);
                return new LiveSessionDto
                {
                    SessionId = session.Id,
                    StrategyId = session.StrategyId,
                    Symbol = session.Symbol,
                    Status = session.Status,
                    StartTime = session.StartTime,
                    StrategyName = strategy.Name
                };
            });
        }
    }
    public void RestartSession(Guid sessionId, Guid strategyId, string symbol, string timeFrame)
    {
        var strategy = _strategyService.GetStrategy(strategyId);
        var symbols = new List<string> { symbol };
        var tf = Enum.Parse<TimeFrame>(timeFrame, true);

        var engine = new TradingEngine(
            _exchange, 
            strategy, 
            symbols, 
            tf, 
            _timerService, 
            _logger,
            TimeSpan.FromSeconds(_tradingSettings.TradingLoopIntervalSeconds)
            );
        _runningEngines[sessionId] = engine;
        engine.StartAsync();
    }
}
