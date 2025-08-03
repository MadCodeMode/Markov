using Markov.API.Models;
using Markov.Services;
using Markov.Services.Engine;
using Markov.Services.Enums;
using Markov.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Markov.API.Services
{
    public class LiveTradingService : ILiveTradingService
    {
        private static readonly ConcurrentDictionary<Guid, ITradingEngine> _runningEngines = new();
        private readonly IStrategyService _strategyService;
        private readonly IExchange _exchange;
        private readonly IServiceProvider _serviceProvider;

        public LiveTradingService(IStrategyService strategyService, IExchange exchange, IServiceProvider serviceProvider)
        {
            _strategyService = strategyService;
            _exchange = exchange;
            _serviceProvider = serviceProvider;
        }

        public Guid StartSession(Guid strategyId, string symbol, string timeFrame)
        {
            var sessionId = Guid.NewGuid();
            RestartSession(sessionId, strategyId, symbol, timeFrame);

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MarkovDbContext>();
                var session = new Markov.Services.Models.LiveSession
                {
                    Id = sessionId,
                    StrategyId = strategyId,
                    Symbol = symbol,
                    TimeFrame = timeFrame,
                    Status = "Running",
                    StartTime = DateTime.UtcNow
                };
                context.LiveSessions.Add(session);
                context.SaveChanges();
            }

            return sessionId;
        }
        
        public void RestartSession(Guid sessionId, Guid strategyId, string symbol, string timeFrame)
        {
            var strategy = _strategyService.GetStrategy(strategyId);
            var symbols = new List<string> { symbol };
            var tf = Enum.Parse<TimeFrame>(timeFrame, true);

            var engine = new TradingEngine(_exchange, strategy, symbols, tf);
            _runningEngines[sessionId] = engine;
            engine.StartAsync();
        }

        public void StopSession(Guid sessionId)
        {
            if (_runningEngines.TryRemove(sessionId, out var engine))
            {
                engine.StopAsync();
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MarkovDbContext>();
                var session = context.LiveSessions.Find(sessionId);
                if (session != null)
                {
                    session.Status = "Stopped";
                    context.SaveChanges();
                }
            }
        }

        public LiveSessionDto GetSession(Guid sessionId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MarkovDbContext>();
                var session = context.LiveSessions.Find(sessionId);

                if (session == null)
                {
                    throw new KeyNotFoundException("Live session not found.");
                }

                return new LiveSessionDto
                {
                    SessionId = session.Id,
                    StrategyId = session.StrategyId,
                    Symbol = session.Symbol,
                    Status = session.Status,
                    StartTime = session.StartTime,
                    StrategyName = _strategyService.GetStrategy(session.StrategyId).Name
                };
            }
        }

        public IEnumerable<LiveSessionDto> GetAllSessions()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MarkovDbContext>();
                var sessions = context.LiveSessions.ToList();
                return sessions.Select(session => new LiveSessionDto
                {
                    SessionId = session.Id,
                    StrategyId = session.StrategyId,
                    Symbol = session.Symbol,
                    Status = session.Status,
                    StartTime = session.StartTime,
                    StrategyName = _strategyService.GetStrategy(session.StrategyId).Name
                });
            }
        }
    }
}