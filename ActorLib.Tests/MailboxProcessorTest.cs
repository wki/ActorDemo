using NUnit.Framework;

namespace ActorLib.Tests;

[TestFixture]
public class MailboxProcessorTest
{
    private MailboxProcessor _mailboxProcessor;
    private TestActor _actor;

    [SetUp]
    public void SetUp()
    {
        _actor = new TestActor();
        _mailboxProcessor = new MailboxProcessor("wx-7", null, _actor);
        _actor.Self = _mailboxProcessor;
        _mailboxProcessor.Start();
    }
    
    [Test]
    public void CanBeInstantiated()
    {
        // Assert
        Assert.IsNotNull(_mailboxProcessor);
        Assert.AreSame(_actor, _mailboxProcessor.Actor);
        Assert.AreEqual("wx-7", _mailboxProcessor.Name);
        Assert.IsNull(_mailboxProcessor.Parent);
        CollectionAssert.IsEmpty(_mailboxProcessor.Children);
        CollectionAssert.IsEmpty(_mailboxProcessor._stash);
    }
    
    [Test]
    public async Task SendMessage_SendsMessageToActor()
    {
        // Act
        _mailboxProcessor.SendMessage(_mailboxProcessor, _mailboxProcessor, new Ping());
        _mailboxProcessor.SendMessage(_mailboxProcessor, _mailboxProcessor, new Pong());
        await Task.Delay(200);
        
        // Assert
        CollectionAssert.AreEqual(new []{"Ping", "Pong"}, _actor.ReceivedMessages);
    }
    
    // child handling
    
    // stash handling
    
    [Test]
    public void ToString_GeneratesName()
    {
        // Assert
        Assert.AreEqual("ActorRef 'wx-7'", _mailboxProcessor.ToString());
    }
}

public class TestActor : Actor
{
    public List<string> ReceivedMessages = new List<string>();
    
    public override async Task OnReceiveAsync(object message)
    {
        ReceivedMessages.Add(message.GetType().Name);
    }
}

public record Ping();

public record Pong();
