using System.Collections.Generic;

namespace Markov.Services.Models
{
    public class ReversalProbability
    {
        public double UpReversalPercentage { get; set; }
        public double DownReversalPercentage { get; set; }
        public List<ReversalDataPoint> UpReversalData { get; set; } = new();
        public List<ReversalDataPoint> DownReversalData { get; set; } = new();
        public List<ReversalDataPoint> UpNonReversalData { get; set; } = new();
        public List<ReversalDataPoint> DownNonReversalData { get; set; } = new();
    }
}