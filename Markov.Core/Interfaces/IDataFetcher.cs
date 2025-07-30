using Markov.Core.Models;

namespace Markov.Core.Interfaces;

public interface IDataFetcher
{
    Task<Asset> FetchDataAsync(string assetName, DateTime from, DateTime to);
}
