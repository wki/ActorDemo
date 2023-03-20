using NUnit.Framework;

namespace ActorLib.Tests;

[TestFixture]
public class ActorSystemTest
{
    private ActorSystem _system;

    [SetUp]
    public void SetUp()
    {
        _system = new ActorSystem("sys");
    }
    
    [Test]
    public void ActorSystem_canBeInstantiated()
    {
        // Assert
        Assert.IsNull(_system.Parent);
        Assert.AreEqual("sys", _system.Name);
        CollectionAssert.IsEmpty(_system.Children);
        Assert.AreSame(_system.Self, _system.MyMailboxProcessor);
    }

    [Test]
    public void ActorSystem_canCreateChildren()
    {
        // Act
        var child = _system.ActorOf<TestActor>("huhu");
        
        // Assert
        Assert.AreSame(child, _system.GetChild("huhu"));
        CollectionAssert.AreEquivalent(
            new[] {child},
            _system.Children
        );
        Assert.AreSame(_system.Self, ((MailboxProcessor)child).Parent);
    }

    [Test]
    public async Task ActorSystem_canAsk()
    {
        // Arrange
        var child = _system.ActorOf<SendActor>("actor", _system.Self);
        
        // Act
        var answer = await _system.Ask<Pong>(child, new Ping());
        
        // Assert
        Assert.IsInstanceOf<Pong>(answer);
    }
}