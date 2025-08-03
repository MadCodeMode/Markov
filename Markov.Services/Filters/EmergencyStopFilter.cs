using Markov.Services.Models;

namespace Markov.Services.Filters;

public class EmergencyStopFilter : ISignalFilter
{
    public string Name => "EmergencyStop";

    public IEnumerable<Signal> Apply(IEnumerable<Signal> signals, IDictionary<string, IEnumerable<Candle>> data)
    {
        // This filter is simple: it stops all signals from passing through.
        // It effectively halts all new trading activity when applied.
        return Enumerable.Empty<Signal>();
    }
}