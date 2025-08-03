using System.Threading.Tasks;
using Markov.Services.Models;

namespace Markov.Services.Interfaces;

    public interface ITradingEngine
    {
        event Action<Order> OnOrderPlaced;
        Task StartAsync();
        Task StopAsync();
    }