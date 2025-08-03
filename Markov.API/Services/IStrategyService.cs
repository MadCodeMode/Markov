using Markov.API.Models;
using Markov.Services.Interfaces;

namespace Markov.API.Services
{
    public interface IStrategyService
    {
        Guid CreateStrategy(CreateStrategyRequest request);
        IStrategy GetStrategy(Guid strategyId);
        IEnumerable<string> GetAvailableStrategies();
        IEnumerable<FilterDto> GetAvailableFilters();
    }
}