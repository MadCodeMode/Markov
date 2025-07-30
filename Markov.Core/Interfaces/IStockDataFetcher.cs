using Markov.Core.Models;
using System;
using System.Threading.Tasks;

namespace Markov.Core.Interfaces;

public interface IStockDataFetcher
{
    Task<Asset> FetchDataAsync(string assetName, DateTime from, DateTime to);
}
