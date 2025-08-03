using Markov.Services.Filters;
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Strategies;
 
 public abstract class BaseStrategy : IStrategy
{
    public abstract string Name { get; }
    protected readonly Dictionary<string, IIndicator> Indicators = new Dictionary<string, IIndicator>();
    private readonly List<ISignalFilter> _filters = new List<ISignalFilter>();

    // This remains the core logic for a specific strategy
    public abstract IEnumerable<Signal> GenerateSignals(IDictionary<string, IEnumerable<Candle>> data);

    protected void AddIndicator(IIndicator indicator)
    {
        Indicators[indicator.Name] = indicator;
    }

    public void AddFilter(ISignalFilter filter)
    {
        if (!_filters.Any(f => f.Name == filter.Name))
        {
            _filters.Add(filter);
        }
    }

    public void RemoveFilter(string filterName)
    {
        _filters.RemoveAll(f => f.Name == filterName);
    }

    public IEnumerable<Signal> GetFilteredSignals(IDictionary<string, IEnumerable<Candle>> data)
    {
        var signals = GenerateSignals(data);
        // Apply all registered filters in sequence
        foreach (var filter in _filters)
        {
            signals = filter.Apply(signals, data);
        }
        return signals;
    }
}