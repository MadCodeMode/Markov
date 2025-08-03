using System.Threading.Tasks;

namespace Markov.Services.Interfaces;

    public interface ITradingEngine
    {
        Task StartAsync();
        Task StopAsync();
    }