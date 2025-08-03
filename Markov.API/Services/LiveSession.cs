using Markov.API.Models;
using Markov.Services.Enums;
using Markov.Services.Interfaces;
using Markov.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Markov.API.Services
{
    public class LiveSession
    {
        private readonly ITradingEngine _engine;
        private readonly List<Trade> _openPositions = new List<Trade>();
        public LiveSessionDto Dto { get; private set; }

        public LiveSession(ITradingEngine engine, Guid strategyId, string strategyName, string symbol)
        {
            _engine = engine;
            Dto = new LiveSessionDto
            {
                SessionId = Guid.NewGuid(),
                StrategyId = strategyId,
                StrategyName = strategyName,
                Symbol = symbol,
                Status = "Running",
                StartTime = DateTime.UtcNow
            };

            _engine.OnOrderPlaced += HandleOrderPlaced;
        }

        private void HandleOrderPlaced(Order order)
        {
            if (order.Side == OrderSide.Buy)
            {
                // For simplicity, we assume any new buy opens a new position.
                // A more complex implementation might handle scaling into positions.
                var newPosition = new Trade
                {
                    Symbol = order.Symbol,
                    Side = OrderSide.Buy,
                    Quantity = order.Quantity,
                    EntryPrice = order.Price,
                    EntryTimestamp = order.Timestamp,
                    StopLoss = order.StopLoss,
                    TakeProfit = order.TakeProfit
                };
                _openPositions.Add(newPosition);
            }
            else if (order.Side == OrderSide.Sell)
            {
                // A sell order closes the first open long position (FIFO).
                var positionToClose = _openPositions.FirstOrDefault(p => p.Side == OrderSide.Buy && p.Symbol == order.Symbol);
                if (positionToClose != null)
                {
                    positionToClose.ExitPrice = order.Price;
                    positionToClose.ExitTimestamp = order.Timestamp;
                    positionToClose.Outcome = TradeOutcome.Closed; // Closed by an opposing signal
                    positionToClose.Pnl = (positionToClose.ExitPrice.Value - positionToClose.EntryPrice) * positionToClose.Quantity;

                    Dto.Trades.Add(new TradeDto
                    {
                        Symbol = positionToClose.Symbol,
                        Side = positionToClose.Side.ToString(),
                        Quantity = positionToClose.Quantity,
                        EntryPrice = positionToClose.EntryPrice,
                        ExitPrice = positionToClose.ExitPrice,
                        Pnl = positionToClose.Pnl,
                        EntryTimestamp = positionToClose.EntryTimestamp,
                        ExitTimestamp = positionToClose.ExitTimestamp,
                        Outcome = positionToClose.Outcome.ToString()
                    });

                    _openPositions.Remove(positionToClose);
                }
            }

            // Recalculate total realized PnL from the closed trades list
            Dto.RealizedPnl = Dto.Trades.Sum(t => t.Pnl);
        }

        public void Start()
        {
            _engine.StartAsync();
        }

        public void Stop()
        {
            _engine.StopAsync();
            Dto.Status = "Stopped";
            _engine.OnOrderPlaced -= HandleOrderPlaced;
        }
    }
}