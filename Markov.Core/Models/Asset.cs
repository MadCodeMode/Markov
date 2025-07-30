namespace Markov.Core.Models;

public class Asset
{
    public required string Name { get; set; }
    public required List<Candle> HistoricalData { get; set; }
}
