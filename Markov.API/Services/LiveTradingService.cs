using Markov.API.Models;
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
        private static readonly ConcurrentDictionary<Guid, (ITradingEngine Engine, LiveSessionDto Session)> _liveSessions = new();
        private readonly IStrategyService _strategyService;
        private readonly IExchange _exchange;

        public LiveTradingService(IStrategyService strategyService, IExchange exchange)
        {
            _strategyService = strategyService;
            _exchange = exchange;
        }

        public Guid StartSession(Guid strategyId, string symbol, string timeFrame)
        {
            var strategy = _strategyService.GetStrategy(strategyId);
            var sessionId = Guid.NewGuid();
            var symbols = new List<string> { symbol };
            var tf = Enum.Parse<TimeFrame>(timeFrame, true);

            var engine = new TradingEngine(_exchange, strategy, symbols, tf);

            var session = new LiveSessionDto
            {
                SessionId = sessionId,
                StrategyId = strategyId,
                StrategyName = strategy.Name,
                Symbol = symbol,
                Status = "Running",
                StartTime = DateTime.UtcNow
            };
            
            _liveSessions[sessionId] = (engine, session);

            engine.StartAsync(); // Start the engine in the background

            return sessionId;
        }

        public void StopSession(Guid sessionId)
        {
            if (_liveSessions.TryGetValue(sessionId, out var sessionInfo))
            {
                sessionInfo.Engine.StopAsync();
                sessionInfo.Session.Status = "Stopped";
            }
            else
            {
                throw new KeyNotFoundException("Live session not found.");
            }
        }

        public LiveSessionDto GetSession(Guid sessionId)
        {
            if (_liveSessions.TryGetValue(sessionId, out var sessionInfo))
            {
                // In a real application, you'd update PnL and trades from a persistent source
                // For this example, we return the session DTO as is.
                return sessionInfo.Session;
            }
            throw new KeyNotFoundException("Live session not found.");
        }

        public IEnumerable<LiveSessionDto> GetAllSessions()
        {
            return _liveSessions.Values.Select(s => s.Session);
        }
    }
}