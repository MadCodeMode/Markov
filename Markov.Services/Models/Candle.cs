namespace Markov.Services.Models;

public class Candle
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Movement Movement { get; set; }
}
