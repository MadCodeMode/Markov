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
    }

    public void ProcessSignal(TradeSignal signal, Candle currentCandle)
    {
        // For this strategy, a signal at candle 'i' is executed at the open of 'i+1'.
        // The signal was generated based on data up to `currentCandle` (index i).
        // The trade is executed on the next candle (index i+1).
        
        int executionIndex = _candles.IndexOf(currentCandle) + 1;
        if (executionIndex >= _candles.Count) return; // Cannot execute on the last candle

        var executionCandle = _candles[executionIndex];
        
        // Much of the logic from your original file's loop body goes here
        // ... (sizing, risk calculation, PnL, etc.) ...
        // This is a simplified example; you would port over your full PnL logic.
        
        var record = new TradeRecord
        {
            Timestamp = executionCandle.Timestamp,
            EntryPrice = executionCandle.Open,
            // Capture the 'why' of the trade
            Notes = string.Join(" | ", signal.TriggerConditions.Select(kv => $"{kv.Key}: {kv.Value}"))
        };

        // ... Add your detailed logic for long/short, TP/SL, and hold moves here ...
        // This part would be a direct port of the logic inside your "Handle LONG" and "Handle SHORT" blocks.

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