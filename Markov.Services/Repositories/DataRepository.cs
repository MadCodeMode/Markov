using Markov.Services.Interfaces;
using Markov.Services.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Markov.Services.Repositories
{
    public class DataRepository : IDataRepository
    {
        private readonly MarkovDbContext _context;

        public DataRepository(MarkovDbContext context)
        {
            _context = context;
        }

        public async Task<Asset> GetAssetAsync(string assetName)
        {
            return await _context.Assets
                .Include(a => a.HistoricalData)
                .FirstOrDefaultAsync(a => a.Name == assetName);
        }

        public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
        {
            return await _context.Assets
                .Include(a => a.HistoricalData)
                .ToListAsync();
        }

        public async Task SaveAssetAsync(Asset asset)
        {
            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();
        }
    }
}
