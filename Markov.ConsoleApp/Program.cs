using Markov.Core.Interfaces;
using Markov.Core.Models;
using Markov.Core.Repositories;
using Markov.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>();
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<BinanceSettings>(context.Configuration.GetSection("Binance"));
        services.AddTransient<IStockDataFetcher, YahooDataFetcher>();
        services.AddTransient<ICryptoDataFetcher, BinanceDataFetcher>();
        services.AddTransient<IDataRepository>(sp => new DataRepository(context.Configuration.GetConnectionString("PostgreSQL")));
        services.AddTransient<IMarkovChainCalculator, MarkovChainCalculator>();
        services.AddTransient<IReversalCalculator, ReversalCalculator>();
    })
    .Build();

var stockDataFetcher = host.Services.GetRequiredService<IStockDataFetcher>();
var cryptoDataFetcher = host.Services.GetRequiredService<ICryptoDataFetcher>();
var dataRepository = host.Services.GetRequiredService<IDataRepository>();
var markovChainCalculator = host.Services.GetRequiredService<IMarkovChainCalculator>();
var reversalCalculator = host.Services.GetRequiredService<IReversalCalculator>();

if (args.Length > 0)
{
    await HandleCommandLineArgs(args);
}
else
{
    await InteractiveMenu();
}

async Task HandleCommandLineArgs(string[] args)
{
    var command = args[0];
    switch (command)
    {
        case "fetch-stock":
            await FetchStockData(args[1]);
            break;
        case "fetch-crypto":
            await FetchCryptoData(args[1]);
            break;
        case "calc-markov":
            await CalculateMarkovChainProbability(args[1], args[2]);
            break;
        case "calc-reversal":
            await CalculateReversalProbability(args[1], int.Parse(args[2]));
            break;
        default:
            Console.WriteLine("Invalid command");
            break;
    }
}

async Task InteractiveMenu()
{
    while (true)
    {
        Console.WriteLine("Choose an option:");
        Console.WriteLine("1. Fetch stock data for an asset");
        Console.WriteLine("2. Fetch crypto data for an asset");
        Console.WriteLine("3. Calculate Markov Chain probability");
        Console.WriteLine("4. Calculate reversal probability");
        Console.WriteLine("5. Exit");

        var option = Console.ReadLine();

        switch (option)
        {
            case "1":
                Console.WriteLine("Enter asset name (e.g. GOOG):");
                var stockAssetName = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(stockAssetName))
                {
                    await FetchStockData(stockAssetName);
                }
                break;
            case "2":
                Console.WriteLine("Enter asset name (e.g. BTCUSDT):");
                var cryptoAssetName = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(cryptoAssetName))
                {
                    await FetchCryptoData(cryptoAssetName);
                }
                break;
            case "3":
                Console.WriteLine("Enter asset name:");
                var markovAssetName = Console.ReadLine();
                Console.WriteLine("Enter pattern (e.g. Up,Down,Up):");
                var patternString = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(markovAssetName) && !string.IsNullOrWhiteSpace(patternString))
                {
                    await CalculateMarkovChainProbability(markovAssetName, patternString);
                }
                break;
            case "4":
                Console.WriteLine("Enter asset name:");
                var reversalAssetName = Console.ReadLine();
                Console.WriteLine("Enter consecutive movements:");
                var consecutiveMovementsString = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(reversalAssetName) && int.TryParse(consecutiveMovementsString, out var consecutiveMovements))
                {
                    await CalculateReversalProbability(reversalAssetName, consecutiveMovements);
                }
                break;
            case "5":
                return;
            default:
                Console.WriteLine("Invalid option");
                break;
        }
    }
}

async Task FetchStockData(string assetName)
{
    var from = new DateTime(2020, 1, 1);
    var to = DateTime.Now;

    var asset = await stockDataFetcher.FetchDataAsync(assetName, from, to);
    await dataRepository.SaveAssetAsync(asset);

    Console.WriteLine($"Stock data for {assetName} fetched and saved successfully.");
}

async Task FetchCryptoData(string assetName)
{
    var from = new DateTime(2020, 1, 1);
    var to = DateTime.Now;

    var asset = await cryptoDataFetcher.FetchDataAsync(assetName, from, to);
    await dataRepository.SaveAssetAsync(asset);

    Console.WriteLine($"Crypto data for {assetName} fetched and saved successfully.");
}

async Task CalculateMarkovChainProbability(string assetName, string patternString)
{
    var asset = await dataRepository.GetAssetAsync(assetName);
    var pattern = patternString.Split(',').Select(s => Enum.Parse<Movement>(s)).ToArray();

    var probability = markovChainCalculator.CalculateNextMovementProbability(asset, pattern);

    Console.WriteLine($"Probability of next movement being Up for {assetName} with pattern {patternString}: {probability:P}");
}

async Task CalculateReversalProbability(string assetName, int consecutiveMovements)
{
    var asset = await dataRepository.GetAssetAsync(assetName);

    var probability = reversalCalculator.CalculateReversalProbability(asset, consecutiveMovements);

    Console.WriteLine($"Probability of reversal for {assetName} after {consecutiveMovements} consecutive movements: {probability:P}");
}
