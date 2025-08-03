
using Markov.Services.Enums;

namespace Markov.Services.Models;

public class Asset
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public AssetType AssetType { get; set; }
    public required string Source { get; set; }
    public required List<Candle> HistoricalData { get; set; }
}
