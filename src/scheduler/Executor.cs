﻿using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace scheduler;
public class Executor
{
    public int Test(){
        return 1;
    }

    private Executor()
    {
    }

    public Executor(ILogger<Executor> logger)
    {
        _logger = logger;
        _jobSchedules = new ConcurrentDictionary<Guid, CancellationTokenSource>();
    }

    private readonly ILogger<Executor> _logger;
    private readonly ConcurrentDictionary<Guid,CancellationTokenSource> _jobSchedules;

    public Guid Schedule(DateTime next, Func<Task> callback){
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
                    await Task.Delay(TimeSpan.FromMilliseconds((UInt32)currentDelay), cancellationTokenSource.Token);
                    delay -= currentDelay;
                }
            }
            catch (AggregateException e)
            {
                _logger.LogError(e, "Aggregate exception while scheduling job ");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception happened while scheduling job");
            }

            try
            {
                await callback();
            }
            catch (AggregateException e)
            {
                _logger.LogError(e, "Aggregate exception while executing callback {scheduleId}", scheduleId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception happened while executing callback {scheduleId}", scheduleId);
            }

            CancellationTokenSource tokenSource;
            if (!_jobSchedules.TryRemove(scheduleId, out tokenSource)){
                _logger.LogError("ScheduleId does not exist in the _jobSchedules {scheduleId}", scheduleId);
            }
        });

        _jobSchedules.TryAdd(scheduleId, cancellationTokenSource);
        return scheduleId;
    }
}
