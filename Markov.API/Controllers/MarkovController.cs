using Markov.API.Models;
using Markov.API.Services;
using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Markov.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarkovController : ControllerBase
    {
        private readonly IStrategyService _strategyService;
        private readonly IBacktestingEngine _backtestingEngine;
        private readonly IExchange _exchange;
        private readonly ILiveTradingService _liveTradingService;

        public MarkovController(
            IStrategyService strategyService,
            IBacktestingEngine backtestingEngine,
            ILiveTradingService liveTradingService,
            IExchange exchange)
        {
            _strategyService = strategyService;
            _backtestingEngine = backtestingEngine;
            _exchange = exchange;
            _liveTradingService = liveTradingService;
        }

        [HttpGet("strategies")]
        public IActionResult GetAvailableStrategies()
        {
            return Ok(_strategyService.GetAvailableStrategies());
        }

        [HttpGet("filters")]
        public IActionResult GetAvailableFilters()
        {
            return Ok(_strategyService.GetAvailableFilters());
        }

        [HttpPost("strategies")]
        public IActionResult CreateStrategy([FromBody] CreateStrategyRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.StrategyName))
            {
                return BadRequest("Strategy name is required.");
            }

            try
            {
                var strategyId = _strategyService.CreateStrategy(request);
                return Ok(new { strategyId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("backtest")]
        public async Task<IActionResult> RunBacktest([FromBody] BacktestRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid backtest request.");
            }

            try
            {
                var strategy = _strategyService.GetStrategy(request.StrategyId);
                var parameters = new BacktestParameters
                {
                    Symbol = request.Symbol,
                    TimeFrame = Enum.Parse<TimeFrame>(request.TimeFrame, true),
                    From = request.From,
                    To = request.To,
                    InitialCapital = request.InitialCapital,
                    Exchange = _exchange
                };

                var result = await _backtestingEngine.RunAsync(strategy, parameters);
                var historicalData = await _exchange.GetHistoricalDataAsync(parameters.Symbol, parameters.TimeFrame, parameters.From, parameters.To);

                var equityCurveData = new List<ChartDataDto>();
                var runningCapital = result.InitialCapital;
                equityCurveData.Add(new ChartDataDto { Timestamp = request.From, Value = runningCapital });

                foreach (var trade in result.Trades.OrderBy(t => t.ExitTimestamp))
                {
                    runningCapital += trade.Pnl;
                    equityCurveData.Add(new ChartDataDto { Timestamp = trade.ExitTimestamp.Value, Value = runningCapital });
                }

                var resultDto = new BacktestResultDto
                {
                    InitialCapital = result.InitialCapital,
                    FinalCapital = result.FinalCapital,
                    RealizedPnl = result.RealizedPnl,
                    WinCount = result.WinCount,
                    LossCount = result.LossCount,
                    HoldCount = result.HoldCount,
                    FinalHeldAssetsValue = result.FinalHeldAssetsValue,
                    WinRate = result.WinCount + result.LossCount > 0 ? (double)result.WinCount / (result.WinCount + result.LossCount) : 0,
                    Trades = result.Trades.Select(t => new TradeDto
                    {
                        Symbol = t.Symbol,
                        Side = t.Side.ToString(),
                        Quantity = t.Quantity,
                        EntryPrice = t.EntryPrice,
                        ExitPrice = t.ExitPrice,
                        Pnl = t.Pnl,
                        EntryTimestamp = t.EntryTimestamp,
                        ExitTimestamp = t.ExitTimestamp,
                        Outcome = t.Outcome.ToString()
                    }).ToList(),
                    Charts = new List<ChartSeriesDto>
                    {
                        new ChartSeriesDto
                        {
                            Name = "Equity Curve",
                            Data = equityCurveData
                        },
                        new ChartSeriesDto
                        {
                            Name = "Asset Price",
                            Data = historicalData.Select(c => new ChartDataDto { Timestamp = c.Timestamp, Value = c.Close }).ToList()
                        }
                    }
                };

                return Ok(resultDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                // In a real application, log this exception
                return StatusCode(500, "An error occurred during the backtest.");
            }
        }


        [HttpPost("live/start")]
        public IActionResult StartLiveSession([FromBody] StartLiveSessionRequest request)
        {
            try
            {
                var sessionId = _liveTradingService.StartSession(request.StrategyId, request.Symbol, request.TimeFrame);
                return Ok(new { sessionId });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("live/{sessionId}/stop")]
        public IActionResult StopLiveSession(Guid sessionId)
        {
            try
            {
                _liveTradingService.StopSession(sessionId);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("live/{sessionId}")]
        public IActionResult GetLiveSession(Guid sessionId)
        {
            try
            {
                return Ok(_liveTradingService.GetSession(sessionId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("live/sessions")]
        public IActionResult GetAllLiveSessions()
        {
            return Ok(_liveTradingService.GetAllSessions());
        }
    }
}