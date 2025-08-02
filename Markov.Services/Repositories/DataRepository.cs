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

        public async Task<Asset> GetAssetAsync(string assetName, DateTime? startDate = null)
        {
            var asset = await _context.Assets
                .Include(a => a.HistoricalData)
                .FirstOrDefaultAsync(a => a.Name == assetName);

            if (asset != null)
            {
                if (startDate != null)
                {
                    asset.HistoricalData = asset.HistoricalData
                        .Where(c => c.Timestamp >= startDate.Value).OrderBy(c => c.Timestamp).ToList();
                }
                else
                {
                    asset.HistoricalData = asset.HistoricalData.OrderBy(c => c.Timestamp).ToList();
                }
            }

            return asset;
        }

        public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
        {
            var assets = await _context.Assets
                .Include(a => a.HistoricalData)
                .ToListAsync();

            foreach (var asset in assets)
            {
                asset.HistoricalData = asset.HistoricalData.OrderBy(c => c.Timestamp).ToList();
            }

            return assets;
        }

        public async Task UpsertAssetAsync(Asset asset)
        {
            var existingAsset = await _context.Assets
                .Include(a => a.HistoricalData)
                .FirstOrDefaultAsync(a => a.Name == asset.Name);

            if (existingAsset == null)
            {
                _context.Assets.Add(asset);
            }
            else
            {
                existingAsset.Name = asset.Name;
                existingAsset.AssetType = asset.AssetType;
                existingAsset.Source = asset.Source;

                var existingCandles = existingAsset.HistoricalData.ToDictionary(c => c.Timestamp);
                var newCandles = new List<Candle>();

                foreach (var candle in asset.HistoricalData)
                {
                    if (existingCandles.TryGetValue(candle.Timestamp, out var existingCandle))
                    {
                        existingCandle.Movement = candle.Movement;
                        existingCandle.Open = candle.Open;
                        existingCandle.Close = candle.Close;
                        existingCandle.High = candle.High;
                        existingCandle.Low = candle.Low;
                        existingCandle.TradeCount = candle.TradeCount;
                        existingCandle.Volume = candle.Volume;
                    }
                    else
                    {
                        newCandles.Add(candle);
                    }
                }
                if(newCandles.Any())
                    existingAsset.HistoricalData.AddRange(newCandles);
            }

            await _context.SaveChangesAsync();
        }
    }
}
