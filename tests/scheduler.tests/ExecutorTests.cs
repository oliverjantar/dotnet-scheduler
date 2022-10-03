using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using scheduler;

namespace scheduler.tests;

public class ExecutorTests
{
    [Fact]
    public void Test1()
    {
        var logger = new NullLogger<Executor>();
        var e = new Executor(logger);
        // Assert.Equal(1,e.Test());
    }
}