namespace Markov.Services.Models
{
    public class ReversalProbability
    {
        public double UpReversalPercentage { get; set; }
        public double DownReversalPercentage { get; set; }
        public List<DateTime> UpReversalDates { get; set; } = new();
        public List<DateTime> DownReversalDates { get; set; } = new();
    }
}