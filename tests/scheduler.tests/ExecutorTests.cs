using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace scheduler.tests;

public class ExecutorTests
{
    private readonly Mock<ILogger<Executor>> _mockLogger;
    private readonly Executor _executor;
    private readonly Mock<Func<CancellationToken, Task>> _mockCallback;

    public ExecutorTests()
    {
        _mockLogger = new Mock<ILogger<Executor>>();
        _executor = new Executor(_mockLogger.Object);
        _mockCallback = new Mock<Func<CancellationToken, Task>>();
    }

    [Fact]
    public async void ExecuteScheduledFunction()
    {
        var next = DateTime.UtcNow;
        var (scheduleId, task) = _executor.Schedule(next, _mockCallback.Object);

        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(_executor.JobSchedules.ContainsKey(scheduleId));

        //Todo: verify logging information
        // _mockLogger.VerifyLogging($"Scheduling a function to be executed at {next.ToLongDateString()}, scheduleId: {scheduleId}",
        //     LogLevel.Information, Times.Once());

        await task;

        Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        Assert.Null(task.Exception);

        _mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));
    }

    [Fact]
    public async void VerifyCallbackIsExecutedEvenWhenTaskIsNotAwaited()
    {
        var (scheduleId, task) = _executor.Schedule(DateTime.UtcNow.AddMilliseconds(100), _mockCallback.Object);

        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(_executor.JobSchedules.ContainsKey(scheduleId));

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        _mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));

        Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        Assert.Null(task.Exception);
    }

    [Fact]
    public async void CancelScheduledFunction()
    {
        var (scheduleId, task) = _executor.Schedule(DateTime.UtcNow.AddDays(1), _mockCallback.Object);

        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(_executor.JobSchedules.ContainsKey(scheduleId));

        Assert.True(_executor.Cancel(scheduleId));

        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));

        await Assert.ThrowsAsync<TaskCanceledException>(() => task); //task ends immediately, it won't wait for 1 day
        Assert.Equal(TaskStatus.Canceled, task.Status);
        Assert.Null(task.Exception);

        _mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async void ExecutedScheduleCannotBeCancelled()
    {
        var (scheduleId, task) = _executor.Schedule(DateTime.UtcNow, _mockCallback.Object);

        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(_executor.JobSchedules.ContainsKey(scheduleId));

        await task;

        Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        Assert.Null(task.Exception);

        _mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));

        Assert.False(_executor.Cancel(scheduleId));
    }

    [Fact]
    public async void DisposeOfSchedulerEndsScheduledTask()
    {
        Task task;
        using (var executor = new Executor(_mockLogger.Object))
        {
            (_, task) = executor.Schedule(DateTime.UtcNow.AddMinutes(1), _mockCallback.Object);
        }

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        Assert.Equal(TaskStatus.Canceled, task.Status);
        Assert.Null(task.Exception);

        _mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async void CancelTaskWhenItExecutesCallback()
    {
        var finished = false;
        var longRunningTask = async (CancellationToken ct) =>
        {
            await Task.Delay(TimeSpan.FromDays(1), ct);
            finished = true;
        };
        
        var (scheduleId, task) = _executor.Schedule(DateTime.UtcNow, longRunningTask);
       
        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(_executor.JobSchedules.ContainsKey(scheduleId));

        Assert.True(_executor.Cancel(scheduleId));

        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));
        
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        Assert.Equal(TaskStatus.Canceled, task.Status);
        Assert.Null(task.Exception);
        Assert.False(finished);
    }

    [Fact(Skip = "NotImplemented")]
    public async void TaskIsAbortedWhenCancelRequestIsIgnored()
    {
        Assert.False(true);
    }

    [Fact]
    public async void ScheduledFunctionThrowsExceptionInExecution()
    {
        var errMessage = "Something horribly went wrong";
        Func<CancellationToken,Task> alwaysFailingTask = (CancellationToken ct) => throw new Exception(errMessage);
        
        var (scheduleId, task) = _executor.Schedule(DateTime.UtcNow.AddMilliseconds(200), alwaysFailingTask);
       
        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(_executor.JobSchedules.ContainsKey(scheduleId));

        await Assert.ThrowsAsync<Exception>(() => task);
        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));
        Assert.Equal(TaskStatus.Faulted, task.Status);
        Assert.Equal(errMessage,task.Exception.InnerExceptions[0].Message);
    }
    
    [Fact(Skip = "NotImplemented")]
    public async void ThrowAggregateExceptionWhenExecutingSchedule()
    {
        Assert.False(true);
    }

    [Fact(Skip = "NotImplemented")]
    public async void CancellAllScheduledFunctions()
    {
        Assert.False(true);
    }

    [Fact(Skip = "NotImplemented")]
    public async void HandleMoreSchedules()
    {
        Assert.False(true);
    }
}