namespace Markov.Services.Time;

public class TimerService : ITimerService
{
    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.Delay(delay, cancellationToken);
    }
}