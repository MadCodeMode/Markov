using Markov.Services.Enums;
using Markov.Services.Models;

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
        var longTermMa = CalculateSma(candles, parameters.LongTermMAPeriod);
        var volumeMa = CalculateVolumeSma(candles, parameters.VolumeMAPeriod);
        var rsi = CalculateRsi(candles, parameters.RsiPeriod);
        var atr = CalculateAtr(candles, parameters.AtrPeriod);

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

    // --- INDICATOR CALCULATION HELPERS ---
private static decimal[] CalculateSma(List<Candle> candles, int period)
{
    // Add guard clause to prevent division by zero
    if (period <= 0) return new decimal[candles.Count];

    var smaValues = new decimal[candles.Count];
    for (int i = period - 1; i < candles.Count; i++)
    {
        decimal sum = 0;
        for (int j = 0; j < period; j++)
        {
            sum += candles[i - j].Close;
        }
        smaValues[i] = sum / period;
    }
    return smaValues;
}

private static decimal[] CalculateVolumeSma(List<Candle> candles, int period)
{
    // Add guard clause to prevent division by zero
    if (period <= 0) return new decimal[candles.Count];

    var volumeSmaValues = new decimal[candles.Count];
    for (int i = period - 1; i < candles.Count; i++)
    {
        decimal sum = 0;
        for (int j = 0; j < period; j++)
        {
            sum += candles[i - j].Volume;
        }
        volumeSmaValues[i] = sum / period;
    }
    return volumeSmaValues;
}

private static decimal[] CalculateAtr(List<Candle> candles, int period)
{
    // Add guard clause for period and data length
    if (period <= 0 || candles.Count < period) return new decimal[candles.Count];

    var atrValues = new decimal[candles.Count];
    decimal[] trValues = new decimal[candles.Count];
    
    // First TR is just High-Low, but we need a previous close for the rest
    trValues[0] = candles[0].High - candles[0].Low;
    for (int i = 1; i < candles.Count; i++)
    {
        decimal tr1 = candles[i].High - candles[i].Low;
        decimal tr2 = Math.Abs(candles[i].High - candles[i - 1].Close);
        decimal tr3 = Math.Abs(candles[i].Low - candles[i - 1].Close);
        trValues[i] = Math.Max(tr1, Math.Max(tr2, tr3));
    }
    
    decimal firstAtr = 0;
    for (int i = 0; i < period; i++) firstAtr += trValues[i];
    atrValues[period - 1] = firstAtr / period;
    
    for (int i = period; i < candles.Count; i++)
    {
        atrValues[i] = (atrValues[i - 1] * (period - 1) + trValues[i]) / period;
    }

    return atrValues;
}

private static decimal[] CalculateRsi(List<Candle> candles, int period)
{
    // Add guard clause for period and data length
    if (period <= 0 || candles.Count <= period) return new decimal[candles.Count];

    var rsiValues = new decimal[candles.Count];
    decimal avgGain = 0;
    decimal avgLoss = 0;

    for (int i = 1; i <= period; i++)
    {
        decimal change = candles[i].Close - candles[i - 1].Close;
        if (change > 0) avgGain += change;
        else avgLoss -= change;
    }
    
    avgGain /= period;
    avgLoss /= period;
    
    if (avgLoss == 0) rsiValues[period] = 100;
    else rsiValues[period] = 100 - (100 / (1 + (avgGain / avgLoss)));
    
    for (int i = period + 1; i < candles.Count; i++)
    {
        decimal change = candles[i].Close - candles[i - 1].Close;
        decimal gain = change > 0 ? change : 0;
        decimal loss = change < 0 ? -change : 0;

        avgGain = (avgGain * (period - 1) + gain) / period;
        avgLoss = (avgLoss * (period - 1) + loss) / period;
        
        if (avgLoss == 0) rsiValues[i] = 100;
        else rsiValues[i] = 100 - (100 / (1 + (avgGain / avgLoss)));
    }

    return rsiValues;
}
}