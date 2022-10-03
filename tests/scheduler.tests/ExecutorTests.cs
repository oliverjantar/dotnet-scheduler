using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace scheduler.tests;

public class ExecutorTests
{
    private Func<CancellationToken,Task> callback = cancellationToken => Task.CompletedTask;
    
    
    [Fact]
    public async void ExecutesScheduledFunction()
    {
        
        var logger = new Mock<ILogger<Executor>>();
        var executor = new Executor(logger.Object);

        var mockCallback = new Mock<Func<CancellationToken, Task>>();

        var next = DateTime.UtcNow;
        
        


        // Assert.Equal(1,e.Test());
    }
}