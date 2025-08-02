using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public interface IDataRepository
{
    Task<Asset> GetAssetAsync(string assetName, DateTime? startDate = null);
    Task<IEnumerable<Asset>> GetAllAssetsAsync();
    Task UpsertAssetAsync(Asset asset);
}
