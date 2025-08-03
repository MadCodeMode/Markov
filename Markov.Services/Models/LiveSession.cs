using System;

namespace Markov.Services.Models;

public class LiveSession
{
    public Guid Id { get; set; }
    public Guid StrategyId { get; set; }
    public string Symbol { get; set; }
    public string TimeFrame { get; set; }
    public string Status { get; set; }
    public DateTime StartTime { get; set; }
}