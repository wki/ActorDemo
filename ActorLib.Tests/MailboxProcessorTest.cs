using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Internal;

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
        CollectionAssert.AreEqual(new []{"wx-7:Ping", "wx-7:Pong"}, _actor.ReceivedMessages);
    }

    [Test]
    public async Task Tell_SendsMessageToReceiver()
    {
        // Arrange
        var receiver = _actor.ActorOf<TestActor>("receiver");
        var sender = _actor.ActorOf<SendActor>("sender", receiver);
        await Task.Delay(200);

        // Act
        ((MailboxProcessor)sender).Actor.Tell(sender, "tell");
        await Task.Delay(200);
        
        // Assert
        CollectionAssert.AreEquivalent(
            new[]{"sender:Ping"},
            ((TestActor)((MailboxProcessor)receiver).Actor).ReceivedMessages
        );
    }
    
    // child handling
    [Test]
    public void ActorOf_WithClearNames_Creates_ChildActors()
    {
        // Act
        var child1 = _actor.ActorOf<TestActor>("child-1");
        var child2 = _actor.ActorOf<TestActor>("child-2");
        var child3 = _actor.ActorOf<TestActor>("child-3");
        
        // Assert
        CollectionAssert.AreEquivalent(
            new[]{child1, child2, child3},
            _actor.Children
        );
        Assert.AreEqual(((MailboxProcessor)child1).Parent, _mailboxProcessor);
        Assert.AreEqual(((MailboxProcessor)child2).Parent, _mailboxProcessor);
        Assert.AreEqual(((MailboxProcessor)child3).Parent, _mailboxProcessor);
    }

    [Test]
    public void ActorOf_WithWildcartNames_Creates_ChildActors()
    {
        // Act
        var child1 = _actor.ActorOf<TestActor>("child-*");
        var child2 = _actor.ActorOf<TestActor>("child-*");

        // Assert
        var name1 = ((MailboxProcessor)child1).Name;
        var name2 = ((MailboxProcessor)child2).Name;
        Assert.AreNotEqual(name1, name2);
        Assert.IsTrue(Regex.IsMatch(name1, "^child-[0-9a-z]{8}$"));
        Assert.IsTrue(Regex.IsMatch(name2, "^child-[0-9a-z]{8}$"));
    }

    [Test]
    public void ActorOf_WithDuplicateName_Throws()
    {
        // Arrange
        var child1 = _actor.ActorOf<TestActor>("foo-bar");
        
        // Act+Assert
        Assert.Throws<ArgumentException>(() => _actor.ActorOf<TestActor>("foo-bar"));
    }
    
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
    
    public override Task OnReceiveAsync(object message)
    {
        var sender = Regex.Replace(Sender.ToString(), "^.*'(.+)'$", "$1");
        ReceivedMessages.Add($"{sender}:{message.GetType().Name}");
        
        return Task.CompletedTask;
    }
}

public class SendActor : Actor
{
    private readonly IActorRef _other;

    public SendActor(IActorRef other)
    {
        _other = other;
    }
    
    public override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            case "tell":
                Tell(_other, new Ping());
                break;
            
            default:
                Forward(_other);
                break;
        }

        return Task.CompletedTask;
    }
}

public class ReplyActor : Actor
{
    public override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            case Ping:
                Reply(new Pong());
                break;
            
            case string s:
                Reply($"answer: {s}");
                break;
        }

        return Task.CompletedTask;
    }
}

public record Ping();

public record Pong();
