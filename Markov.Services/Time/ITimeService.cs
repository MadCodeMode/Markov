namespace Markov.Services.Time;

public interface ITimerService
{
    Task Delay(TimeSpan delay, CancellationToken cancellationToken);
}