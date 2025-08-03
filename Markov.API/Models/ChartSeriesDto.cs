using System.Collections.Generic;

namespace Markov.API.Models
{
    public class ChartSeriesDto
    {
        public string Name { get; set; }
        public List<ChartDataDto> Data { get; set; } = new List<ChartDataDto>();
    }
}