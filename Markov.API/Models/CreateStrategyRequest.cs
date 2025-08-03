using System.Collections.Generic;

namespace Markov.API.Models
{
    public class CreateStrategyRequest
    {
        public string StrategyName { get; set; } = string.Empty;
        public List<FilterDto> Filters { get; set; } = new List<FilterDto>();
    }
}