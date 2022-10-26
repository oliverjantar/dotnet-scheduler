using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("scheduler.tests")]

namespace scheduler;

/// <summary>
/// Class responsible for scheduling and executing functions at a given time.
/// </summary>
public class Executor : IDisposable
{
    private readonly ILogger<Executor> _logger;
    internal readonly ConcurrentDictionary<Guid, (Task, CancellationTokenSource)> JobSchedules;
    internal bool _isDisposed;

    public Executor(ILogger<Executor> logger)
    {
        _logger = logger;
        JobSchedules = new ConcurrentDictionary<Guid, (Task, CancellationTokenSource)>();
    }

    /// <summary>
    /// Schedules a function to be executed at a given time.
    /// </summary>
    /// <param name="next">DateTime when callback will be executed</param>
    /// <param name="callback">Function to be executed</param>
    /// <returns>Id of the scheduled function. Task of a scheduled function - it is used only in tests, in production it works as fire and forget</returns>
    public (Guid, Task) Schedule(DateTime next, Func<CancellationToken, Task> callback)
    {
        if (_isDisposed)
            throw new ObjectDisposedException("Executor");
        
        var cancellationTokenSource = new CancellationTokenSource();
        var scheduleId = Guid.NewGuid();

        _logger.LogInformation("Scheduling a function to be executed at {next}, scheduleId: {scheduleId}", next,
            scheduleId);

        TimeSpan timeSpan = next - DateTime.UtcNow;

        var task = Task.Run(async delegate
        {
            try
            {
                //Task.Delay takes UInt32, max value is ~49 days. If task is scheduled for more than that, it needs to be delayed several times in a loop.
                var delay = timeSpan.TotalMilliseconds;
                while (delay > 0)
                {
                    var currentDelay = delay > UInt32.MaxValue - 1 ? UInt32.MaxValue - 1 : delay;
                    await Task.Delay(TimeSpan.FromMilliseconds((UInt32)currentDelay), cancellationTokenSource.Token);
                    delay -= currentDelay;
                }

                _logger.LogInformation("Executing function, scheduleId: {scheduleId}", scheduleId);
                await callback(cancellationTokenSource.Token);
                _logger.LogInformation("Function executed successfully, scheduleId: {scheduleId}", scheduleId);
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                {
                    _logger.LogError(e, "Scheduled function was cancelled, scheduleId: {scheduleId}",
                        scheduleId);
                }
                else if (e is AggregateException ex)
                {
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        _logger.LogError(innerEx,
                            "Aggregate exception while executing function, scheduleId: {scheduleId}",
                            scheduleId);
                    }
                }
                else
                {
                    _logger.LogError(e,
                        "Exception happened while scheduling or executing function, scheduleId: {scheduleId}",
                        scheduleId);
                }

                throw;
            }
            finally
            {
                if (!JobSchedules.TryRemove(scheduleId, out _))
                {
                    _logger.LogError("ScheduleId does not exist in the JobSchedules, scheduleId: {scheduleId}",
                        scheduleId);
                }
            }
        }, cancellationTokenSource.Token);

        JobSchedules.TryAdd(scheduleId, (task, cancellationTokenSource));
        return (scheduleId, task);
    }

    public bool Cancel(Guid scheduleId)
    {
        if (_isDisposed)
            throw new ObjectDisposedException("Executor");
        
        if (!JobSchedules.TryRemove(scheduleId, out var value) || !value.Item2.Token.CanBeCanceled) return false;
        _logger.LogInformation("Cancelling scheduled function, scheduleId: {scheduleId}", scheduleId);
        value.Item2.Cancel();
        
        return true;
    }

    internal void CancelAll()
    {
        if (_isDisposed)
            throw new ObjectDisposedException("Executor");
        
        foreach (var schedule in JobSchedules)
            Cancel(schedule.Key);
    }

    public void Dispose()
    {
        if (!_isDisposed)
            CancelAll();

        _isDisposed = true;
    }
}