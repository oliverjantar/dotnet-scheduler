using System;
using System.Collections.Generic;
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

        _mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));
    }

    [Fact]
    public async void VerifyCallbackIsExecutedEvenWhenTaskIsNotAwaited()
    {
        var (scheduleId, _) = _executor.Schedule(DateTime.UtcNow.AddMilliseconds(100), _mockCallback.Object);

        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(_executor.JobSchedules.ContainsKey(scheduleId));

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        _mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));
    }

    [Fact]
    public async void CancelScheduledFunction()
    {
        var (scheduleId, task) = _executor.Schedule(DateTime.UtcNow.AddDays(1), _mockCallback.Object);

        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(_executor.JobSchedules.ContainsKey(scheduleId));
        
        Assert.True(_executor.Cancel(scheduleId));

        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));
        
        await Assert.ThrowsAsync<TaskCanceledException>(()=> task); //task ends immediately, it won't wait for 1 day
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

        _mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(_executor.JobSchedules.ContainsKey(scheduleId));

        Assert.False(_executor.Cancel(scheduleId));
    }
    
    [Fact(Skip = "NotImplemented")]
    public async void DisposeOfSchedulerEndsScheduledTasks()
    {
        
        Task task1, task2;
        CancellationTokenSource cts1, cts2;
        {
            var executor = new Executor(_mockLogger.Object);
            (var scheduleId1, task1) = executor.Schedule(DateTime.UtcNow.AddMinutes(1), _mockCallback.Object);
            (var scheduleId2, task2) = executor.Schedule(DateTime.UtcNow.AddMinutes(2), _mockCallback.Object);

            // cts1 = executor.JobSchedules[scheduleId1];
            // cts2 = executor.JobSchedules[scheduleId2];
        }
        
        Assert.True(task1.IsCanceled);
        Assert.True(task2.IsCanceled);
    }
    
    [Fact(Skip = "NotImplemented")]
    public async void CancelTaskWhenItExecutesCallback()
    {
        Assert.False(true);
    }
    
    [Fact(Skip = "NotImplemented")]
    public async void TaskIsAbortedWhenCancelRequestIsIgnored()
    {
        Assert.False(true);
    }

    [Fact(Skip = "NotImplemented")]
    public async void ScheduledFunctionThrowsExceptionInExecution()
    {
        Assert.False(true);
    }

    [Fact(Skip = "NotImplemented")]
    public async void CancellAllScheduledFunctions()
    {
        Assert.False(true);
    }
    
    [Fact(Skip = "NotImplemented")]
    public async void CancelScheduledFunctionDuringExecution()
    {
        Assert.False(true);
    }
    
    [Fact(Skip = "NotImplemented")]
    public async void HandleMoreSchedules()
    {
        Assert.False(true);
    }

  
}