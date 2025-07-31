using Markov.Services.Interfaces;
using Markov.Services.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Markov.Services.Repositories
{
    public class DataRepository : IDataRepository
    {
        public Task<Asset> GetAssetAsync(string assetName)
        {
            // TODO: Implement the logic to get an asset from the database
            return Task.FromResult<Asset>(null);
        }

        public Task<IEnumerable<Asset>> GetAllAssetsAsync()
        {
            // Return an empty list for now
            return Task.FromResult<IEnumerable<Asset>>(new List<Asset>());
        }

        public Task SaveAssetAsync(Asset asset)
        {
            // TODO: Implement the logic to save an asset to the database
            return Task.CompletedTask;
        }
    }
}
