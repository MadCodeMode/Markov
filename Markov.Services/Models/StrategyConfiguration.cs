using System;
using System.Collections.Generic;

namespace Markov.Services.Models;

public class StrategyConfiguration
{
    public Guid Id { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public string FiltersJson { get; set; } = string.Empty;
}