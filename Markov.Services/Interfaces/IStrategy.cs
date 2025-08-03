using Markov.Services.Filters;
using Markov.Services.Models;

namespace Markov.Services.Interfaces;

public interface IStrategy
    {
        string Name { get; }

        // Raw, unfiltered signal generation
        IEnumerable<Signal> GenerateSignals(IDictionary<string, IEnumerable<Candle>> data);

        // Methods for managing filters
        void AddFilter(ISignalFilter filter);
        void RemoveFilter(string filterName);

        // Method to get the final, filtered signals
        IEnumerable<Signal> GetFilteredSignals(IDictionary<string, IEnumerable<Candle>> data);
    }