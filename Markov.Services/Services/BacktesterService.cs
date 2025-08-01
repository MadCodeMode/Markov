
using Markov.Services.Interfaces;
using Markov.Services.Models;

namespace Markov.Services.Services;

public class BacktesterService
{
    private readonly IReversalCalculator _reversalCalculator;

    public BacktesterService(IReversalCalculator reversalCalculator)
    {
        _reversalCalculator = reversalCalculator;
    }

    public BacktestResult Run(Asset asset, BacktestParameters parameters)
    {
        // --- 1. Initialization ---
        var tradeHistory = new List<TradeRecord>();
        decimal tradingCapital = parameters.StartingCapital;
        decimal holdAccount = 0m;
        decimal realizedPnl = 0m;
        decimal dailyHoldYield = parameters.HoldAccountAnnualYield / 365.25m;

        int consecutiveMovements = parameters.ConsecutiveMovements;
        var candles = asset.HistoricalData; // Assuming asset.HistoricalData is List<Candle>
        int requiredDataLength = consecutiveMovements + 1;

        // --- 2. Main Backtesting Loop ---
        for (int i = 0; i <= candles.Count - requiredDataLength; i++)
        {
            // Always compound hold account interest daily
            holdAccount *= (1 + dailyHoldYield);

            var firstMovement = candles[i].Movement;
            bool isSequence = true;

            // Check if we have a valid sequence
            for (int j = 1; j < consecutiveMovements; j++)
            {
                if (candles[i + j].Movement != firstMovement)
                {
                    isSequence = false;
                    break;
                }
            }

            if (!isSequence) continue; // No sequence, move to the next day

            // --- 3. Sequence Found - Execute Trade Logic ---
            var outcomeCandle = candles[i + consecutiveMovements];

            // Determine trade size
            decimal tradeSize = parameters.TradeSizeMode == TradeSizeMode.FixedAmount
                ? parameters.TradeSizeFixedAmount
                : tradingCapital * parameters.TradeSizePercentage;

            if (tradeSize > tradingCapital)
            {
                tradeSize = tradingCapital; // Not enough capital, use all that's left
            }

            if (tradeSize <= 0) continue; // No capital left to trade

            var record = new TradeRecord
            {
                Timestamp = outcomeCandle.Timestamp,
                Signal = $"{firstMovement} Reversal Signal after {consecutiveMovements} moves",
                AmountInvested = tradeSize
            };

            // A. Handle LONG trade (after a DOWN sequence)
            if (firstMovement == Movement.Down)
            {
                if (outcomeCandle.Movement == Movement.Up) // Successful reversal
                {
                    record.Outcome = TradeOutcome.TakeProfit;
                    record.EntryPrice = outcomeCandle.Open;
                    record.ExitPrice = outcomeCandle.Close;
                    decimal pnl = (record.ExitPrice - record.EntryPrice) * (tradeSize / record.EntryPrice);
                    decimal fees = tradeSize * parameters.TradeFeePercentage * 2; // Entry and exit fee
                    record.Pnl = pnl - fees;

                    tradingCapital += record.Pnl * parameters.ReinvestmentPercentage;
                    realizedPnl += record.Pnl * (1 - parameters.ReinvestmentPercentage);
                }
                else // Failed reversal, move funds to hold account
                {
                    record.Outcome = TradeOutcome.MovedToHold;
                    tradingCapital -= tradeSize;
                    holdAccount += tradeSize;
                    record.Pnl = -tradeSize; // Representing the opportunity cost / moved capital
                    record.Notes = $"${tradeSize:F2} moved to hold account.";
                }
            }
            // B. Handle SHORT trade (after an UP sequence)
            else if (firstMovement == Movement.Up)
            {
                if (outcomeCandle.Movement == Movement.Down) // Successful reversal signal
                {
                    record.EntryPrice = outcomeCandle.Open;
                    decimal stopLossPrice = record.EntryPrice * (1 + parameters.StopLossPercentage);

                    if (outcomeCandle.High >= stopLossPrice) // Stop-loss was hit
                    {
                        record.Outcome = TradeOutcome.StopLoss;
                        record.ExitPrice = stopLossPrice;
                        record.Notes = $"Stop-loss triggered at {stopLossPrice:F2} (Day High: {outcomeCandle.High:F2})";
                    }
                    else // Take-profit at close
                    {
                        record.Outcome = TradeOutcome.TakeProfit;
                        record.ExitPrice = outcomeCandle.Close;
                    }

                    decimal pnl = (record.EntryPrice - record.ExitPrice) * (tradeSize / record.EntryPrice);
                    decimal fees = tradeSize * parameters.TradeFeePercentage * 2; // Entry and exit fee
                    record.Pnl = pnl - fees;

                    tradingCapital += record.Pnl * parameters.ReinvestmentPercentage;
                    realizedPnl += record.Pnl * (1 - parameters.ReinvestmentPercentage);
                }
                else // Failed short signal (Up -> Up), do nothing
                {
                    record.Outcome = TradeOutcome.Continuation;
                    record.Pnl = 0;
                    record.Notes = "Short signal failed to trigger. No action taken.";
                }
            }

            tradeHistory.Add(record);

            // Skip the processed candles to avoid overlapping signals
            i += consecutiveMovements - 1;
        }

        // --- 4. Finalization ---
        // Run the original calculator just to get the probability stats
        var reversalAnalysis = _reversalCalculator.CalculateReversalProbability(asset, parameters);

        return new BacktestResult
        {
            StartingCapital = parameters.StartingCapital,
            FinalTradingCapital = tradingCapital,
            FinalHoldAccountBalance = holdAccount,
            RealizedPNL = realizedPnl,
            TradeHistory = tradeHistory,
            ReversalAnalysis = reversalAnalysis
        };
    }
}