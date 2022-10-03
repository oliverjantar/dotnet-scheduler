using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace scheduler;
/// <summary>
/// Class responsible for scheduling and executing functions at a given time.
/// </summary>
public class Executor
{ 
    private readonly ILogger<Executor> _logger;
    private readonly ConcurrentDictionary<Guid, CancellationToken> _jobSchedules;
    public Executor(ILogger<Executor> logger)
    {
        _logger = logger;
        _jobSchedules = new ConcurrentDictionary<Guid, CancellationToken>();
    }
    /// <summary>
    /// Schedules a function to be executed at a given time.
    /// </summary>
    /// <param name="next">DateTime when callback will be executed</param>
    /// <param name="callback">Function to be executed</param>
    /// <param name="ct">Cancellation token for this method, not the inner scheduled function</param>
    /// <returns>Id of the scheduled function</returns>
    public Guid Schedule(DateTime next, Func<CancellationToken, Task> callback, CancellationToken ct){
        var cancellationTokenSource = new CancellationTokenSource();
        var scheduleId = Guid.NewGuid();

        TimeSpan timeSpan = next - DateTime.UtcNow;

        Task.Run(async delegate
        {
            try
            {
                //Task.Delay takes UInt32, max value is ~49 days. If task is scheduled for more than that, it needs to be delayed several times in a loop.
                var delay = (double)timeSpan.TotalMilliseconds;
                while (delay > 0)
                {
                    var currentDelay = delay > UInt32.MaxValue - 1 ? UInt32.MaxValue - 1 : delay;
                    await Task.Delay(TimeSpan.FromMilliseconds((UInt32)currentDelay), ct);
                    delay -= currentDelay;
                }
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions) 
                {
                    _logger.LogError(ex, "Aggregate exception while scheduling job");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception happened while scheduling job");
            }

            try
            {
                await callback(cancellationTokenSource.Token);
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions) 
                {
                    _logger.LogError(e, "Aggregate exception while executing callback {scheduleId}", scheduleId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception happened while executing callback {scheduleId}", scheduleId);
            }

            if (!_jobSchedules.TryRemove(scheduleId, out _)){
                _logger.LogError("ScheduleId does not exist in the _jobSchedules {scheduleId}", scheduleId);
            }
        }, cancellationTokenSource.Token);

        _jobSchedules.TryAdd(scheduleId, cancellationTokenSource.Token);
        return scheduleId;
    }
}
