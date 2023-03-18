namespace ActorDemo.Tests;
using NUnit.Framework;

[TestFixture]
public class TaskTest
{
    [Test]
    [Repeat(1000)]
    public async Task ContinueWith_CreatesANewTask_ForCompletedTask()
    {
        // Arrange
        var currentThreadId = Thread.CurrentThread.ManagedThreadId;
        var continueThreadId = -1;
        
        // Act
        await Task.CompletedTask
            .ContinueWith(task =>
            {
                continueThreadId = Thread.CurrentThread.ManagedThreadId;
            }, TaskContinuationOptions.RunContinuationsAsynchronously);
        
        // Assert
        Assert.AreNotEqual(-1, continueThreadId);
        Assert.AreNotEqual(currentThreadId, continueThreadId);
    }
}