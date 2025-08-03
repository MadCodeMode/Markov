using System;

namespace Markov.Services.Models;

public class LiveSession
{
    public Guid Id { get; set; }
    public Guid StrategyId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string TimeFrame { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}