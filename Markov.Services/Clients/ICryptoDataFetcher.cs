using Markov.Services.Models;
using System;
using System.Threading.Tasks;

namespace Markov.Services.Interfaces;

public interface ICryptoDataFetcher
{
    Task<Asset> FetchDataAsync(string assetName, DateTime from, DateTime to);
}
