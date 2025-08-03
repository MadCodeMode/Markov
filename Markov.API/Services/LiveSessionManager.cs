using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Markov.API.Services
{
    public class LiveSessionManager : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public LiveSessionManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<Markov.Services.MarkovDbContext>();
                var liveTradingService = scope.ServiceProvider.GetRequiredService<ILiveTradingService>();

                var runningSessions = context.LiveSessions.Where(s => s.Status == "Running").ToList();

                foreach (var session in runningSessions)
                {
                    // This method will be added to the ILiveTradingService to handle the restart logic
                    liveTradingService.RestartSession(session.Id, session.StrategyId, session.Symbol, session.TimeFrame);
                }
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Cleanly stop all running engines on application shutdown
            using (var scope = _serviceProvider.CreateScope())
            {
                var liveTradingService = scope.ServiceProvider.GetRequiredService<ILiveTradingService>();
                var allSessions = liveTradingService.GetAllSessions();
                foreach (var session in allSessions.Where(s => s.Status == "Running"))
                {
                    liveTradingService.StopSession(session.SessionId);
                }
            }
            return Task.CompletedTask;
        }
    }
}