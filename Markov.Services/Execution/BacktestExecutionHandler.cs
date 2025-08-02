using Markov.Services.Models;

namespace Markov.Services.Execution;

public class BacktestExecutionHandler : IExecutionHandler
{
    private readonly BacktestParameters _params;
    private readonly List<Candle> _candles;
    private readonly decimal[] _atr;
    private readonly List<TradeRecord> _tradeHistory = new();
    
    private decimal _tradingCapital;
    private decimal _holdAccountAssetQuantity = 0m;
    private decimal _realizedPnl = 0m;
    private int _winCount = 0;
    private int _lossCount = 0;
    private int _holdMoveCount = 0;

    public BacktestExecutionHandler(BacktestParameters parameters, List<Candle> candles, decimal[] atr)
    {
        _params = parameters;
        _candles = candles;
        _atr = atr;
        _tradingCapital = parameters.StartingCapital;
        
        if (_candles.Count != _atr.Length)
            throw new ArgumentException("ATR array must match candle count.");
    }

    public void ProcessSignal(TradeSignal signal, Candle currentCandle, int currentIndex)
    {
        int executionIndex = _candles.IndexOf(currentCandle) + 1;
        if (executionIndex >= _candles.Count) return;

        var executionCandle = _candles[executionIndex];

         // Defensive check
        if (_atr.Length <= executionIndex)
        {
            // Log or skip if misaligned
            return;
        }

        if (_params.EnableAtrTargets && _atr[executionIndex] <= 0)
        {
            return; // Skip trade if ATR is invalid
        }


        decimal tradeSize = _params.TradeSizeMode == TradeSizeMode.FixedAmount
            ? _params.TradeSizeFixedAmount
            : _tradingCapital * _params.TradeSizePercentage;


        if (tradeSize > _tradingCapital) tradeSize = _tradingCapital;
        if (tradeSize <= 0) return;

        var record = new TradeRecord
        {
            Timestamp = executionCandle.Timestamp,
            Signal = $"{signal.Type} Signal",
            AmountInvested = tradeSize,
            EntryPrice = executionCandle.Open,
            Notes = string.Join(" | ", signal.TriggerConditions.Select(kv => $"{kv.Key}: {kv.Value}"))
        };

        if (signal.Type == SignalType.Long)
        {
            decimal takeProfitPrice = _params.EnableAtrTargets
                ? record.EntryPrice + (_atr[executionIndex] * _params.TakeProfitAtrMultiplier)
                : record.EntryPrice * (1 + _params.TakeProfitPercentage);

            if (executionCandle.High >= takeProfitPrice)
            {
                record.Outcome = TradeOutcome.TakeProfit;
                record.ExitPrice = takeProfitPrice;
            }
            else
            {
                record.Outcome = TradeOutcome.CloseOut;
                record.ExitPrice = executionCandle.Close;
            }

            decimal pnl = (record.ExitPrice - record.EntryPrice) * (tradeSize / record.EntryPrice);
            decimal fees = tradeSize * _params.TradeFeePercentage * 2;
            decimal finalPnl = pnl - fees;

            if (finalPnl < 0)
            {
                record.Outcome = TradeOutcome.MovedToHold;
                record.Pnl = 0;
                decimal assetQuantityBought = tradeSize / record.EntryPrice;
                _holdAccountAssetQuantity += assetQuantityBought;
                _tradingCapital -= tradeSize;
                record.Notes += $" | Moved {assetQuantityBought:F8} of asset to hold account.";
                _holdMoveCount++;
            }
            else
            {
                record.Pnl = finalPnl;
                _tradingCapital += record.Pnl * _params.ReinvestmentPercentage;
                _realizedPnl += record.Pnl * (1 - _params.ReinvestmentPercentage);
                if (record.Pnl > 0) _winCount++;
                else _lossCount++;
            }
        }
        else if (signal.Type == SignalType.Short)
        {
            decimal stopLossPrice = _params.EnableAtrTargets
                ? record.EntryPrice + (_atr[executionIndex] * _params.StopLossAtrMultiplier)
                : record.EntryPrice * (1 + _params.StopLossPercentage);

            decimal takeProfitPrice = _params.EnableAtrTargets
                ? record.EntryPrice - (_atr[executionIndex] * _params.TakeProfitAtrMultiplier)
                : record.EntryPrice * (1 - _params.TakeProfitPercentage);

            if (executionCandle.High >= stopLossPrice)
            {
                record.Outcome = TradeOutcome.StopLoss;
                record.ExitPrice = stopLossPrice;
            }
            else if (executionCandle.Low <= takeProfitPrice)
            {
                record.Outcome = TradeOutcome.TakeProfit;
                record.ExitPrice = takeProfitPrice;
            }
            else
            {
                record.Outcome = TradeOutcome.CloseOut;
                record.ExitPrice = executionCandle.Close;
            }

            decimal pnl = (record.EntryPrice - record.ExitPrice) * (tradeSize / record.EntryPrice);
            decimal fees = tradeSize * _params.TradeFeePercentage * 2;
            record.Pnl = pnl - fees;

            _tradingCapital += record.Pnl * _params.ReinvestmentPercentage;
            _realizedPnl += record.Pnl * (1 - _params.ReinvestmentPercentage);

            if (record.Pnl > 0) _winCount++;
            else _lossCount++;
        }

        _tradeHistory.Add(record);
    }

    public BacktestResult GetResult()
    {
        decimal finalHoldValue = _candles.Any()
            ? _holdAccountAssetQuantity * _candles.Last().Close
            : 0;

        return new BacktestResult
        {
            StartingCapital = _params.StartingCapital,
            FinalTradingCapital = _tradingCapital,
            FinalHoldAccountAssetQuantity = _holdAccountAssetQuantity,
            FinalHoldAccountValue = finalHoldValue,
            RealizedPNL = _realizedPnl,
            TradeHistory = _tradeHistory,
            WinCount = _winCount,
            LossCount = _lossCount,
            HoldMoveCount = _holdMoveCount
        };
    }
}