using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Services;
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
        private readonly BacktesterService _backtesterService;
        private readonly ILogger<MarkovController> _logger;

        public MarkovController(
            ICryptoDataFetcher cryptoDataFetcher,
            IDataRepository dataRepository,
            IMarkovChainCalculator markovChainCalculator,
            IReversalCalculator reversalCalculator,
            ILogger<MarkovController> logger,
            BacktesterService backtesterService)
        {
            _cryptoDataFetcher = cryptoDataFetcher;
            _dataRepository = dataRepository;
            _markovChainCalculator = markovChainCalculator;
            _reversalCalculator = reversalCalculator;
            _logger = logger;
            _backtesterService = backtesterService;
        }

        [HttpGet("assets")]
        public async Task<IActionResult> GetSavedData()
        {
            var assets = await _dataRepository.GetAssetAsync("BTCUSDT");
            _logger.LogInformation(string.Join('\n', assets));

            return Ok(assets);
        }

        [HttpPost("fetch-crypto/{assetName}")]
        public async Task<IActionResult> FetchCryptoData(string assetName, [FromQuery] DateTime startDate, [FromQuery] DateTime? endDate = null)
        {
            var asset = await _cryptoDataFetcher.FetchDataAsync(assetName, startDate, endDate ?? DateTime.Now);

            await _dataRepository.UpsertAssetAsync(asset);

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

            var pattern = patternString.Split(',').Select(Enum.Parse<Movement>).ToArray();

            var probability = _markovChainCalculator.CalculateNextMovementProbability(asset, pattern);

            return Ok($"Probability of next movement being Up for {assetName} with pattern {patternString}: {probability:P}");
        }

        [HttpPost("calc-reversal/{assetName}")]
        public async Task<IActionResult> CalculateReversalProbability(
            string assetName,
            [FromBody] BacktestParameters parameters)
        {
            var asset = await _dataRepository.GetAssetAsync(assetName);
            if (asset == null)
            {
                return NotFound($"Asset {assetName} not found.");
            }

            var probabilities = _reversalCalculator.CalculateReversalProbability(asset, parameters);

            return Ok(probabilities);
        }

                [HttpPost("calc-reversal/{assetName}/backtest")]
        public async Task<IActionResult> BackTestAsset(
            string assetName,
            [FromBody] BacktestParameters parameters)
        {
            var asset = await _dataRepository.GetAssetAsync(assetName);
            if (asset == null)
            {
                return NotFound($"Asset {assetName} not found.");
            }

            var probabilities = _backtesterService.Run(asset, parameters);

            return Ok(probabilities);
        }
    }
}
