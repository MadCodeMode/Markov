using Markov.Services.Models;

namespace Markov.Services.Filters;

public interface ISignalFilter
{
    string Name { get; }
    IEnumerable<Signal> Apply(IEnumerable<Signal> signals, IDictionary<string, IEnumerable<Candle>> data);
}