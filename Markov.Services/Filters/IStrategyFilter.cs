namespace Markov.Services.Interfaces;

public interface IStrategyFilter
{
    /// <summary>
    /// Returns true if the signal passes the filter. If false, no trade signal should be generated.
    /// </summary>
    bool IsValid(int index, out string reason);
}
