using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public interface IDataFetcher
{
    Task<Asset> FetchDataAsync(string assetName, DateTime from, DateTime to);
}
