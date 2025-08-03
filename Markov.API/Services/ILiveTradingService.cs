using Markov.API.Models;
using System;
using System.Collections.Generic;

namespace Markov.API.Services
{
    public interface ILiveTradingService
    {
        Guid StartSession(Guid strategyId, string symbol, string timeFrame);
        void StopSession(Guid sessionId);
        LiveSessionDto GetSession(Guid sessionId);
        IEnumerable<LiveSessionDto> GetAllSessions();
    }
}