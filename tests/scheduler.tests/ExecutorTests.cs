using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace scheduler.tests;

public class ExecutorTests
{
    [Fact]
    public async void ExecutesScheduledFunction()
    {
        var logger = new Mock<ILogger<Executor>>();
        var executor = new Executor(logger.Object);
        var mockCallback = new Mock<Func<CancellationToken, Task>>();

        var (scheduleId, task) = executor.Schedule(DateTime.UtcNow, mockCallback.Object);

        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(executor.JobSchedules.ContainsKey(scheduleId));

        await task;

        mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(executor.JobSchedules.ContainsKey(scheduleId));
    }
    
    [Fact]
    public async void VerifyCallbackIsExecutedEvenWhenTaskIsNotAwaited()
    {
        var logger = new Mock<ILogger<Executor>>();
        var executor = new Executor(logger.Object);
        var mockCallback = new Mock<Func<CancellationToken, Task>>();

        var (scheduleId, _) = executor.Schedule(DateTime.UtcNow.AddMilliseconds(100), mockCallback.Object);

        Assert.NotEqual(Guid.Empty, scheduleId);
        Assert.True(executor.JobSchedules.ContainsKey(scheduleId));

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        mockCallback.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(executor.JobSchedules.ContainsKey(scheduleId));
    }
}