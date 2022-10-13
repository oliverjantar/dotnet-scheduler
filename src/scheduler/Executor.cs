using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("scheduler.tests")]

namespace scheduler;

/// <summary>
/// Class responsible for scheduling and executing functions at a given time.
/// </summary>
public class Executor
{
    private readonly ILogger<Executor> _logger;
    internal readonly ConcurrentDictionary<Guid, CancellationTokenSource> JobSchedules;

    public Executor(ILogger<Executor> logger)
    {
        _logger = logger;
        JobSchedules = new ConcurrentDictionary<Guid, CancellationTokenSource>();
    }

    /// <summary>
    /// Schedules a function to be executed at a given time.
    /// </summary>
    /// <param name="next">DateTime when callback will be executed</param>
    /// <param name="callback">Function to be executed</param>
    /// <returns>Id of the scheduled function. Task of a scheduled function - it is used only in tests, in production it works as fire and forget</returns>
    public (Guid, Task) Schedule(DateTime next, Func<CancellationToken, Task> callback)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var scheduleId = Guid.NewGuid();

        TimeSpan timeSpan = next - DateTime.UtcNow;

        var task = Task.Run(async delegate
        {
            try
            {
                //Task.Delay takes UInt32, max value is ~49 days. If task is scheduled for more than that, it needs to be delayed several times in a loop.
                var delay = timeSpan.TotalMilliseconds;
                _logger.LogInformation("Executing {scheduleId} in {delay}ms", scheduleId, delay);
                while (delay > 0)
                {
                    var currentDelay = delay > UInt32.MaxValue - 1 ? UInt32.MaxValue - 1 : delay;
                    await Task.Delay(TimeSpan.FromMilliseconds((UInt32)currentDelay), cancellationTokenSource.Token);
                    delay -= currentDelay;
                }
            }
            catch (AggregateException e)
            {
                //can this happen, isn't exception enough?
                foreach (var ex in e.InnerExceptions)
                {
                    _logger.LogError(ex, "Aggregate exception while scheduling job {scheduleId}", scheduleId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception happened while scheduling job {scheduleId}", scheduleId);
            }

            try
            {
                _logger.LogInformation("Executing callback for schedule {scheduleId}", scheduleId);
                await callback(cancellationTokenSource.Token);
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions)
                {
                    //handle properly if task was cancelled
                    _logger.LogError(e, "Aggregate exception while executing callback {scheduleId}", scheduleId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception happened while executing callback {scheduleId}", scheduleId);
            }

            if (!JobSchedules.TryRemove(scheduleId, out _))
            {
                _logger.LogError("ScheduleId does not exist in the _jobSchedules {scheduleId}", scheduleId);
            }
        }, cancellationTokenSource.Token);

        JobSchedules.TryAdd(scheduleId, cancellationTokenSource);
        return (scheduleId, task);
    }

    public bool Cancel(Guid scheduleId)
    {
        if (!JobSchedules.TryRemove(scheduleId, out var cts) || !cts.Token.CanBeCanceled) return false;
        cts.Cancel();
        return true;
    }
}