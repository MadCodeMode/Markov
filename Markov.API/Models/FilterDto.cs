using System.Collections.Generic;

namespace Markov.API.Models
{
    public class FilterDto
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}