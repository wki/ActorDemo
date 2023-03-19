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
        
        CollectionAssert.IsEmpty(_actor.ReceivedMessages);
        CollectionAssert.AreEquivalent(
            new []{"AfterStart"},
            _actor.CalledHooks
        );
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
        var sender = _actor.ActorOf<SendActor>("sender", _mailboxProcessor);
        await Task.Delay(200);

        // Act
        ((MailboxProcessor)sender).Actor.Tell(sender, "tell");
        await Task.Delay(200);
        
        // Assert
        CollectionAssert.AreEquivalent(
            new[]{"sender:Ping"},
            _actor.ReceivedMessages
        );
    }

    [Test]
    public async Task Reply_AnswersMessageBackToSender()
    {
        // Arrange
        var replyer = _actor.ActorOf<SendActor>("replyer", _mailboxProcessor);
        
        // Act
        _actor.Tell(replyer, new Ping());
        await Task.Delay(200);
        
        // Assert
        CollectionAssert.AreEquivalent(
            new[]{"replyer:Pong"},
            _actor.ReceivedMessages
        );
    }

    [Test]
    public async Task Forward_SendsMessageToReceiverWithoutChangingSender()
    {
        // Arrange
        var replyer = _actor.ActorOf<SendActor>("forwarder", _mailboxProcessor);
        
        // Act
        _actor.Tell(replyer, new Something());
        await Task.Delay(200);
        
        // Assert
        CollectionAssert.AreEquivalent(
            new[]{"wx-7:Something"},
            _actor.ReceivedMessages
        );
    }

    [Test]
    public async Task ExceptionDuringProcessing_HandlesError()
    {
        // Act
        _mailboxProcessor.SendMessage(_mailboxProcessor, _mailboxProcessor, "die");
        await Task.Delay(800); // restartPolicy needs 500ms
        
        // Assert
        CollectionAssert.AreEquivalent(
            new[]{"AfterStart", "BeforeRestart: ArgumentException", "AfterRestart"},
            _actor.CalledHooks
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

    [Test]
    public void GetChild_NoChildren_givesNull()
    {
        // Assert
        Assert.IsNull(_mailboxProcessor.GetChild("foo"));
    }

    [Test]
    public void GetChild_unknown_givesNull()
    {
        // Arrange
        var child1 = _actor.ActorOf<TestActor>("child-1");
        var child2 = _actor.ActorOf<TestActor>("child-2");
        var child3 = _actor.ActorOf<TestActor>("child-3");

        // Assert
        Assert.IsNull(_mailboxProcessor.GetChild("foo"));        
    }
    
    [Test]
    public void GetChild_known_givesReference()
    {
        // Arrange
        var child1 = _actor.ActorOf<TestActor>("child-1");
        var child2 = _actor.ActorOf<TestActor>("child-2");
        var child3 = _actor.ActorOf<TestActor>("child-3");

        // Act
        var child = _mailboxProcessor.GetChild("child-2");
        
        // Assert
        Assert.AreSame(child, child2);        
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
    public List<string> CalledHooks = new List<string>();
    
    public override Task OnReceiveAsync(object message)
    {
        var sender = Regex.Replace(Sender.ToString(), "^.*'(.+)'$", "$1");
        ReceivedMessages.Add($"{sender}:{message.GetType().Name}");

        if (message == "die")
            throw new ArgumentException("died just for fun");
        
        return Task.CompletedTask;
    }

    public override void AfterStart() => CalledHooks.Add("AfterStart");
    public override void BeforeRestart(Exception e, object m) => CalledHooks.Add($"BeforeRestart: {e.GetType().Name}");
    public override void AfterRestart() => CalledHooks.Add("AfterRestart");
    public override void BeforeStop() => CalledHooks.Add("AfterStart");
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
            
            case Ping:
                Reply(new Pong());
                break;

            default:
                Forward(_other);
                break;
        }

        return Task.CompletedTask;
    }
}

public record Ping();

public record Pong();

public record Something();

