using Markov.Services.Interfaces;
using Markov.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Markov.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarkovController : ControllerBase
    {
        private readonly ICryptoDataFetcher _cryptoDataFetcher;
        private readonly IDataRepository _dataRepository;
        private readonly IMarkovChainCalculator _markovChainCalculator;
        private readonly IReversalCalculator _reversalCalculator;

        public MarkovController(
            ICryptoDataFetcher cryptoDataFetcher,
            IDataRepository dataRepository,
            IMarkovChainCalculator markovChainCalculator,
            IReversalCalculator reversalCalculator)
        {
            _cryptoDataFetcher = cryptoDataFetcher;
            _dataRepository = dataRepository;
            _markovChainCalculator = markovChainCalculator;
            _reversalCalculator = reversalCalculator;
        }

        [HttpGet("assets")]
        public async Task<IActionResult> GetSavedData()
        {
            var assets = await _dataRepository.GetAllAssetsAsync();
            return Ok(assets);
        }

        [HttpPost("fetch-crypto/{assetName}")]
        public async Task<IActionResult> FetchCryptoData(string assetName)
        {
            var from = new DateTime(2020, 1, 1);
            var to = DateTime.Now;

            var asset = await _cryptoDataFetcher.FetchDataAsync(assetName, from, to);
            await _dataRepository.SaveAssetAsync(asset);

            return Ok($"Crypto data for {assetName} fetched and saved successfully.");
        }

        [HttpGet("calc-markov/{assetName}")]
        public async Task<IActionResult> CalculateMarkovChainProbability(string assetName, [FromQuery] string patternString)
        {
            var asset = await _dataRepository.GetAssetAsync(assetName);
            if (asset == null)
            {
                return NotFound($"Asset {assetName} not found.");
            }

            var pattern = patternString.Split(',').Select(s => Enum.Parse<Movement>(s)).ToArray();

            var probability = _markovChainCalculator.CalculateNextMovementProbability(asset, pattern);

            return Ok($"Probability of next movement being Up for {assetName} with pattern {patternString}: {probability:P}");
        }

        [HttpGet("calc-reversal/{assetName}")]
        public async Task<IActionResult> CalculateReversalProbability(string assetName, [FromQuery] int consecutiveMovements)
        {
            var asset = await _dataRepository.GetAssetAsync(assetName);
            if (asset == null)
            {
                return NotFound($"Asset {assetName} not found.");
            }

            var probability = _reversalCalculator.CalculateReversalProbability(asset, consecutiveMovements);

            return Ok($"Probability of reversal for {assetName} after {consecutiveMovements} consecutive movements: {probability:P}");
        }
    }
}
