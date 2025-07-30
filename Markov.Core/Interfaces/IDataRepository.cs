using Markov.Core.Models;

namespace Markov.Core.Interfaces;

public interface IDataRepository
{
    Task<Asset> GetAssetAsync(string assetName);
    Task SaveAssetAsync(Asset asset);
}
