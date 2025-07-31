using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public interface IDataRepository
{
    Task<Asset> GetAssetAsync(string assetName);
    Task<IEnumerable<Asset>> GetAllAssetsAsync();
    Task SaveAssetAsync(Asset asset);
}
