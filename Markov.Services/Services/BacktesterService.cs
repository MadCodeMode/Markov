using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Services;

public class BacktesterService
{
    // The IReversalCalculator dependency was in the original but not used,
    // so I have removed it from the constructor for this example.
    // public BacktesterService(IReversalCalculator reversalCalculator) { ... }

    public BacktestResult Run(Asset asset, BacktestParameters parameters)
    {
        // --- 1. Initialization ---
        var tradeHistory = new List<TradeRecord>();
        decimal tradingCapital = parameters.StartingCapital;
        decimal holdAccount = 0m;
        decimal realizedPnl = 0m;
        decimal dailyHoldYield = parameters.HoldAccountAnnualYield / 365.25m;

        int consecutiveMovements = parameters.ConsecutiveMovements;
        var candles = asset.HistoricalData;
        int requiredDataLength = consecutiveMovements + 1;

        // --- 2. Main Backtesting Loop ---
        for (int i = 0; i <= candles.Count - requiredDataLength; i++)
        {
            holdAccount *= (1 + dailyHoldYield);

            var firstMovement = candles[i].Movement;
            bool isSequence = true;
            for (int j = 1; j < consecutiveMovements; j++)
            {
                if (candles[i + j].Movement != firstMovement)
                {
                    isSequence = false;
                    break;
                }
            }

            if (!isSequence) continue;

            // --- 3. Sequence Found - Execute Realistic Trade ---
            // The decision to trade is made here, BEFORE looking at the outcome.
            var outcomeCandle = candles[i + consecutiveMovements];

            decimal tradeSize = parameters.TradeSizeMode == TradeSizeMode.FixedAmount
                ? parameters.TradeSizeFixedAmount
                : tradingCapital * parameters.TradeSizePercentage;

            if (tradeSize > tradingCapital) tradeSize = tradingCapital;
            if (tradeSize <= 0) continue;

            var record = new TradeRecord
            {
                Timestamp = outcomeCandle.Timestamp,
                Signal = $"{firstMovement} Reversal Signal after {consecutiveMovements} moves",
                AmountInvested = tradeSize,
                EntryPrice = outcomeCandle.Open // <<< Entry is ALWAYS at the Open
            };

            // A. Handle LONG trade (after a DOWN sequence)
            if (firstMovement == Movement.Down)
            {
                decimal takeProfitPrice = record.EntryPrice * (1 + parameters.TakeProfitPercentage);

                // Determine exit: Check if TP was hit during the day.
                if (outcomeCandle.High >= takeProfitPrice)
                {
                    record.Outcome = TradeOutcome.TakeProfit;
                    record.ExitPrice = takeProfitPrice;
                }
                else // If not, exit at the day's close.
                {
                    record.Outcome = TradeOutcome.CloseOut;
                    record.ExitPrice = outcomeCandle.Close;
                }

                // Calculate PnL based on the realistic entry and exit
                decimal pnl = (record.ExitPrice - record.EntryPrice) * (tradeSize / record.EntryPrice);
                decimal fees = tradeSize * parameters.TradeFeePercentage * 2;
                record.Pnl = pnl - fees;

                // Update capital based on PnL
                tradingCapital += record.Pnl * parameters.ReinvestmentPercentage;
                realizedPnl += record.Pnl * (1 - parameters.ReinvestmentPercentage);

                if (record.Pnl < 0)
                {
                    // Set PnL to 0 (loss is not realized), and move traded amount to hold account
                    record.Pnl = 0;

                    decimal amountToMove = Math.Min(tradeSize, tradingCapital);
                    if (amountToMove > 0)
                    {
                        tradingCapital -= amountToMove;
                        holdAccount += amountToMove;
                        record.Notes = $"Trade loss ignored. Moved ${amountToMove:F2} to hold account instead.";
                    }
                }
            }
            // B. Handle SHORT trade (after an UP sequence)
            else if (firstMovement == Movement.Up)
            {
                decimal stopLossPrice = record.EntryPrice * (1 + parameters.StopLossPercentage);
                decimal takeProfitPrice = record.EntryPrice * (1 - parameters.TakeProfitPercentage);

                // Determine exit (Priority: SL > TP > Close)
                if (outcomeCandle.High >= stopLossPrice)
                {
                    record.Outcome = TradeOutcome.StopLoss;
                    record.ExitPrice = stopLossPrice;
                }
                else if (outcomeCandle.Low <= takeProfitPrice)
                {
                    record.Outcome = TradeOutcome.TakeProfit;
                    record.ExitPrice = takeProfitPrice;
                }
                else
                {
                    record.Outcome = TradeOutcome.CloseOut;
                    record.ExitPrice = outcomeCandle.Close;
                }

                // Calculate PnL for the short position
                decimal pnl = (record.EntryPrice - record.ExitPrice) * (tradeSize / record.EntryPrice);
                decimal fees = tradeSize * parameters.TradeFeePercentage * 2;
                record.Pnl = pnl - fees;

                // Update capital (no special hold account penalty for shorts)
                tradingCapital += record.Pnl * parameters.ReinvestmentPercentage;
                realizedPnl += record.Pnl * (1 - parameters.ReinvestmentPercentage);
            }

            tradeHistory.Add(record);
            i += consecutiveMovements - 1; // Skip ahead to avoid overlapping signals
        }
        
        return new BacktestResult
        {
            StartingCapital = parameters.StartingCapital,
            FinalTradingCapital = tradingCapital,
            FinalHoldAccountBalance = holdAccount,
            RealizedPNL = realizedPnl,
            TradeHistory = tradeHistory
        };
    }
}