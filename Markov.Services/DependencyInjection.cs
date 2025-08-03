using Markov.Services.Engine;
using Markov.Services.Exchanges;
using Markov.Services.Interfaces;
using Markov.Services.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Markov.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddTradingServices(this IServiceCollection services)
        {
            // --- Exchange Configuration (Singleton) ---
            // The underlying exchange connection (DummyExchange in this case) is registered as a singleton.
            // This is critical because a real exchange client manages persistent connections (like WebSockets)
            // and holds state (like API keys). We only want one instance of this client for the entire application lifetime.
            services.AddSingleton<DummyExchange>();

            // The CachingExchange, which is a decorator, is also registered as a singleton.
            // It gets the singleton DummyExchange injected. This ensures that all parts of the application
            // share the same cache and the same underlying exchange connection.
            services.AddSingleton<IExchange, CachingExchange>(provider =>
                new CachingExchange(provider.GetRequiredService<DummyExchange>()));

            
            // --- Strategy Configuration (Transient) ---
            // Strategies are registered as transient. This is the most flexible approach.
            // A new instance of SimpleMacdStrategy will be created every time IStrategy is requested.
            // This is important because it ensures that filters added for one backtest or trading session
            // do not affect another. Each operation gets a fresh, clean strategy instance to configure.
            services.AddTransient<IStrategy, SimpleMacdStrategy>();


            // --- Engine Configuration (Transient) ---
            // The core engines are registered as transient. A "run" of an engine is a discrete operation.
            // You might run many backtests with different parameters, so you want a new BacktestingEngine instance
            // for each run. Similarly, while a TradingEngine might run for a long time, registering it as
            // transient provides the flexibility to create multiple, independent trading bots if needed in the future.
            services.AddTransient<ITradingEngine, TradingEngine>();
            services.AddTransient<IBacktestingEngine, BacktestingEngine>();


            // --- What is NOT Registered and Why ---
            //
            // 1. INDICATORS (SmaIndicator, AtrIndicator, etc.):
            //    These are considered implementation details of a strategy. The BaseStrategy is responsible
            //    for creating its own instances of the indicators it needs. They are components, not services.
            //
            // 2. FILTERS (TrendFilter, RsiFilter, AtrTargetsFilter, etc.):
            //    Filters are a key part of the user's dynamic configuration. They are not static services.
            //    The user creates instances of these filters at runtime (`new TrendFilter(50)`) and applies
            //    them to a specific strategy instance. Registering them in the DI container would make this
            //    dynamic, on-the-fly configuration impossible. The application code, not the DI container,
            //    is responsible for creating and managing the lifecycle of filters.

            
            return services;
        }
    }
}