namespace scheduler;

public class CronSchedule
{
    private readonly Executor _executor;
    public CronSchedule(Executor executor) => _executor = executor; 
    
    public async Task Schedule(string next, Func<CancellationToken,Task> callback, CancellationToken ct)
    {

        while (true)
        {
            
            ct.ThrowIfCancellationRequested();
            var (scheduleId, task) = _executor.Schedule(DateTime.Now, callback);
            
            
        }

    }
}