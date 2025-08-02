using Markov.Services.Models;
using Markov.Services.Helpers;

namespace Markov.Services.Services;

public class BacktesterService
{
    public BacktestResult Run(Asset asset, BacktestParameters parameters)
    {
        // --- 1. Initialization ---
        var tradeHistory = new List<TradeRecord>();
        decimal tradingCapital = parameters.StartingCapital;
        decimal holdAccountAssetQuantity = 0m;
        decimal realizedPnl = 0m;

        int winCount = 0;
        int lossCount = 0;
        int holdMoveCount = 0;

        var candles = asset.HistoricalData;
        int consecutiveMovements = parameters.ConsecutiveMovements;
        int requiredDataLength = consecutiveMovements + 1;

        // --- PRE-CALCULATE INDICATORS for efficiency ---
        var longTermMa = IndicatorCalculatorHelpers.CalculateSma(candles, parameters.LongTermMAPeriod);
        var volumeMa = IndicatorCalculatorHelpers.CalculateVolumeSma(candles, parameters.VolumeMAPeriod);
        var rsi = IndicatorCalculatorHelpers.CalculateRsi(candles, parameters.RsiPeriod);
        var atr = IndicatorCalculatorHelpers.CalculateAtr(candles, parameters.AtrPeriod);

        // --- 2. Main Backtesting Loop ---
        for (int i = 0; i <= candles.Count - requiredDataLength; i++)
        {
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

            // --- 3. Sequence Found - Apply Filters Before Trading ---
            int signalIndex = i + consecutiveMovements - 1; // Index of the last candle in the sequence

            // FILTER 1: Trend Filter
            if (parameters.EnableTrendFilter)
            {
                decimal currentPrice = candles[signalIndex].Close;
                decimal maValue = longTermMa[signalIndex];
                if (firstMovement == Movement.Down && currentPrice < maValue) continue; // Don't buy a dip in a downtrend
                if (firstMovement == Movement.Up && currentPrice > maValue) continue; // Don't short a rally in an uptrend
            }

            // FILTER 2: RSI Filter
            if (parameters.EnableRsiFilter)
            {
                decimal rsiValue = rsi[signalIndex];
                if (firstMovement == Movement.Down && rsiValue > parameters.RsiOversoldThreshold) continue; // Not oversold enough
                if (firstMovement == Movement.Up && rsiValue < parameters.RsiOverboughtThreshold) continue; // Not overbought enough
            }

            // FILTER 3: Volume Filter
            if (parameters.EnableVolumeFilter)
            {
                decimal volumeValue = candles[signalIndex].Volume;
                decimal maVolumeValue = volumeMa[signalIndex];
                if (volumeValue < maVolumeValue * parameters.MinVolumeMultiplier) continue; // Not a high-volume climax
            }

            // --- 4. Filters Passed - Execute Trade ---
            var outcomeCandle = candles[i + consecutiveMovements];
            int outcomeIndex = i + consecutiveMovements;

            decimal tradeSize = parameters.TradeSizeMode == TradeSizeMode.FixedAmount
                ? parameters.TradeSizeFixedAmount
                : tradingCapital * parameters.TradeSizePercentage;
            
            // Prevent trading if ATR is zero and ATR targets are enabled.
            if (parameters.EnableAtrTargets && atr[outcomeIndex] <= 0)
            {
                continue; // Skip this trade, invalid TP/SL levels.
            }

            if (tradeSize > tradingCapital) tradeSize = tradingCapital;
            if (tradeSize <= 0) continue;

            var record = new TradeRecord
            {
                Timestamp = outcomeCandle.Timestamp,
                Signal = $"{firstMovement} Reversal Signal after {consecutiveMovements} moves",
                AmountInvested = tradeSize,
                EntryPrice = outcomeCandle.Open
            };

            // A. Handle LONG trade (after a DOWN sequence)
            if (firstMovement == Movement.Down)
            {
                decimal takeProfitPrice = parameters.EnableAtrTargets
                    ? record.EntryPrice + (atr[outcomeIndex] * parameters.TakeProfitAtrMultiplier)
                    : record.EntryPrice * (1 + parameters.TakeProfitPercentage);

                if (outcomeCandle.High >= takeProfitPrice)
                {
                    record.Outcome = TradeOutcome.TakeProfit;
                    record.ExitPrice = takeProfitPrice;
                }
                else
                {
                    record.Outcome = TradeOutcome.CloseOut;
                    record.ExitPrice = outcomeCandle.Close;
                }

                decimal pnl = (record.ExitPrice - record.EntryPrice) * (tradeSize / record.EntryPrice);
                decimal fees = tradeSize * parameters.TradeFeePercentage * 2;
                decimal finalPnl = pnl - fees;

                // ** NEW HOLD LOGIC **
                if (finalPnl < 0)
                {
                    // A failed long trade is not a loss; it's a move to a long-term hold.
                    record.Outcome = TradeOutcome.MovedToHold;
                    record.Pnl = 0; // The trade has zero PnL impact.

                    decimal assetQuantityBought = tradeSize / record.EntryPrice;
                    holdAccountAssetQuantity += assetQuantityBought;
                    tradingCapital -= tradeSize; // Capital is removed from the trading pool.
                    record.Notes = $"Moved {assetQuantityBought:F8} of asset to hold account.";
                    holdMoveCount++;
                }
                else
                {
                    // A successful trade realizes PnL as normal.
                    record.Pnl = finalPnl;
                    tradingCapital += record.Pnl * parameters.ReinvestmentPercentage;
                    realizedPnl += record.Pnl * (1 - parameters.ReinvestmentPercentage);

                    if (record.Pnl > 0) winCount++;
                    else if (record.Pnl < 0) lossCount++;
                }
            }
            // B. Handle SHORT trade (after an UP sequence)
            else if (firstMovement == Movement.Up)
            {
                decimal stopLossPrice = parameters.EnableAtrTargets
                    ? record.EntryPrice + (atr[outcomeIndex] * parameters.StopLossAtrMultiplier)
                    : record.EntryPrice * (1 + parameters.StopLossPercentage);

                decimal takeProfitPrice = parameters.EnableAtrTargets
                    ? record.EntryPrice - (atr[outcomeIndex] * parameters.TakeProfitAtrMultiplier)
                    : record.EntryPrice * (1 - parameters.TakeProfitPercentage);

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

                decimal pnl = (record.EntryPrice - record.ExitPrice) * (tradeSize / record.EntryPrice);
                decimal fees = tradeSize * parameters.TradeFeePercentage * 2;
                record.Pnl = pnl - fees;

                tradingCapital += record.Pnl * parameters.ReinvestmentPercentage;
                realizedPnl += record.Pnl * (1 - parameters.ReinvestmentPercentage);
                
                if (record.Pnl > 0) winCount++;
                else if (record.Pnl < 0) lossCount++;
            }

            tradeHistory.Add(record);
            i += consecutiveMovements - 1;
        }
        
        // Calculate the final dollar value of the held assets using the last known price
        decimal finalHoldValue = candles.Any() 
            ? holdAccountAssetQuantity * candles.Last().Close 
            : 0;

        return new BacktestResult
        {
            StartingCapital = parameters.StartingCapital,
            FinalTradingCapital = tradingCapital,
            FinalHoldAccountAssetQuantity = holdAccountAssetQuantity,
            FinalHoldAccountValue = finalHoldValue,
            RealizedPNL = realizedPnl,
            TradeHistory = tradeHistory,
            WinCount = winCount,
            LossCount = lossCount,
            HoldMoveCount = holdMoveCount
        };
    }
}