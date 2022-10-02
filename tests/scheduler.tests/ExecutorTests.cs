using Xunit;
using scheduler;

namespace scheduler.tests;

public class ExecutorTests
{
    [Fact]
    public void Test1()
    {
        var e = new Executor();
        Assert.Equal(1,e.Test());
    }
}