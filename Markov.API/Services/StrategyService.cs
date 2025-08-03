using Markov.API.Models;
using Markov.Services.Filters;
using Markov.Services.Interfaces;
using Markov.Services.Strategies;
using Markov.Trading.Engine.Filters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Markov.API.Services
{
    public class StrategyService : IStrategyService
    {
        private static readonly ConcurrentDictionary<Guid, IStrategy> _strategies = new ConcurrentDictionary<Guid, IStrategy>();
        private readonly IServiceProvider _serviceProvider;

        public StrategyService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Guid CreateStrategy(CreateStrategyRequest request)
        {
            var strategy = CreateStrategyInstance(request.StrategyName);
            if (strategy == null)
            {
                throw new ArgumentException("Invalid strategy name");
            }

            foreach (var filterDto in request.Filters)
            {
                var filter = CreateFilterInstance(filterDto);
                if (filter != null)
                {
                    strategy.AddFilter(filter);
                }
            }

            var strategyId = Guid.NewGuid();
            _strategies[strategyId] = strategy;
            return strategyId;
        }

        public IStrategy GetStrategy(Guid strategyId)
        {
            if (_strategies.TryGetValue(strategyId, out var strategy))
            {
                return strategy;
            }
            throw new KeyNotFoundException("Strategy not found");
        }
        
        public IEnumerable<string> GetAvailableStrategies()
        {
            return new List<string>
            {
                "Simple MACD Strategy"
            };
        }

        public IEnumerable<FilterDto> GetAvailableFilters()
        {
            return new List<FilterDto>
            {
                new FilterDto { Name = "TrendFilter", Parameters = new Dictionary<string, object> { { "longTermMAPeriod", 200 } } },
                new FilterDto { Name = "RsiFilter", Parameters = new Dictionary<string, object> { { "rsiPeriod", 14 }, { "overboughtThreshold", 70 }, { "oversoldThreshold", 30 } } },
                new FilterDto { Name = "VolumeFilter", Parameters = new Dictionary<string, object> { { "volumeMAPeriod", 20 }, { "minVolumeMultiplier", 1.5 } } },
                new FilterDto { Name = "AtrTargetsFilter", Parameters = new Dictionary<string, object> { { "atrPeriod", 14 }, { "takeProfitAtrMultiplier", 2.0 }, { "stopLossAtrMultiplier", 1.5 } } },
                new FilterDto { Name = "TakeProfitStopLossFilter", Parameters = new Dictionary<string, object> { { "takeProfitPercentage", 0.10 }, { "stopLossPercentage", 0.05 }, { "useHoldStrategyForLongs", false } } },
                new FilterDto { Name = "EmergencyStopFilter", Parameters = new Dictionary<string, object>() }
            };
        }

        private IStrategy CreateStrategyInstance(string strategyName)
        {
            return strategyName switch
            {
                "Simple MACD Strategy" => new SimpleMacdStrategy(),
                _ => null
            };
        }

        private ISignalFilter CreateFilterInstance(FilterDto filterDto)
        {
            return filterDto.Name switch
            {
                "TrendFilter" => new TrendFilter(GetParameter<int>(filterDto.Parameters, "longTermMAPeriod")),
                "RsiFilter" => new RsiFilter(GetParameter<int>(filterDto.Parameters, "rsiPeriod"), GetParameter<decimal>(filterDto.Parameters, "overboughtThreshold"), GetParameter<decimal>(filterDto.Parameters, "oversoldThreshold")),
                "VolumeFilter" => new VolumeFilter(GetParameter<int>(filterDto.Parameters, "volumeMAPeriod"), GetParameter<decimal>(filterDto.Parameters, "minVolumeMultiplier")),
                "AtrTargetsFilter" => new AtrTargetsFilter(GetParameter<int>(filterDto.Parameters, "atrPeriod"), GetParameter<decimal>(filterDto.Parameters, "takeProfitAtrMultiplier"), GetParameter<decimal>(filterDto.Parameters, "stopLossAtrMultiplier")),
                "TakeProfitStopLossFilter" => new TakeProfitStopLossFilter(GetParameter<decimal>(filterDto.Parameters, "takeProfitPercentage"), GetParameter<decimal>(filterDto.Parameters, "stopLossPercentage"), GetParameter<bool>(filterDto.Parameters, "useHoldStrategyForLongs")),
                "EmergencyStopFilter" => new EmergencyStopFilter(),
                _ => null
            };
        }

        private T GetParameter<T>(Dictionary<string, object> parameters, string key)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                if (value is JsonElement element)
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText());
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            return default;
        }
    }
}