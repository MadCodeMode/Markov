
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

public class AccountBalance
{
    public required string Asset { get; set; }
    public decimal Free { get; set; }
    public decimal Locked { get; set; }
    public decimal Total => Free + Locked;
}
