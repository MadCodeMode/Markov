using Markov.Services.Models;
using Markov.Services.Helpers;
using Markov.Services.Interfaces;
using Markov.Services.Strategies.Filters;

namespace Markov.Services.Strategies;

public abstract class StrategyBase : ITradingStrategy
    {
        protected List<Candle> Candles { get; private set; } = new();
        protected BacktestParameters Parameters { get; }
        protected IIndicatorProvider Indicators { get; private set; } = default!;
        protected readonly List<IStrategyFilter> Filters = new();
        protected Movement? ConsecutiveMovementsDirection { get; set; }


        // Pre-calculated arrays (can still be cached here if you prefer)
    protected decimal[] LongTermMA => Indicators.GetSma(Parameters.LongTermMAPeriod);
        protected decimal[] VolumeMA => Indicators.GetVolumeSma(Parameters.VolumeMAPeriod);
        protected decimal[] Rsi => Indicators.GetRsi(Parameters.RsiPeriod);
        protected decimal[] Atr => Indicators.GetAtr(Parameters.AtrPeriod);

        protected StrategyBase(BacktestParameters parameters)
        {
            Parameters = parameters;
        }

        public virtual void Initialize(List<Candle> history)
        {
            Candles = history;
            Indicators = new IndicatorProvider(Candles); // default implementation
        }

        protected void RegisterCommonFilters()
        {
            if (Parameters.EnableTrendFilter)
                Filters.Add(new TrendFilter(Parameters, Candles, Indicators));
            
            // if (Parameters.EnableRsiFilter)
            //     Filters.Add(new RsiFilter(Parameters, Indicators));
            
            if (Parameters.EnableVolumeFilter)
                Filters.Add(new VolumeFilter(Parameters, Candles, Indicators));
        }


        /// <summary>
        /// Applies all registered filters. If any fails, returns false and accumulates reasons.
        /// </summary>
        protected bool ApplyFilters(int index, out Dictionary<string, string> filterReasons)
        {
            filterReasons = new Dictionary<string, string>();

            foreach (var filter in Filters)
            {
                if (!filter.IsValid(index, out var reason)) return false;
                filterReasons.Add(filter.GetType().Name.Replace("Filter", ""), reason);
            }

            return true;
        }

        public abstract TradeSignal? GenerateSignal(int currentIndex);
    }