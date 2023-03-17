using ActorDemo.Routing;
using ActorDemo.Ask;

namespace ActorDemo;

/// <summary>
/// Base Class for every Actor and ActorSystem (contains user code for actor)
/// </summary>
public abstract class Actor : IActorBuilder
{
    /// <summary>
    /// Sender of the message currently processed
    /// </summary>
    public IActorRef Sender { get; set; }

    /// <summary>
    /// my own reference (actually to our MailboxProcessor)
    /// </summary>
    public IActorRef Self { get; internal set; }
    
    // convenient and type-safe access to our Mailbox Processor
    internal MailboxProcessor MyMailboxProcessor => Self as MailboxProcessor; 

    public string Name => MyMailboxProcessor.Name;

    public IActorRef Parent => MyMailboxProcessor.Parent;

    public IReadOnlyList<IActorRef> Children => MyMailboxProcessor.Children;

    protected Actor() {}

    /// <summary>
    /// Build a (child) actor
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IActorRef ActorOf<T>(string name, params object[] args)
        where T : Actor => MyMailboxProcessor.ActorOf<T>(name, args);
    // {
    //     if (name.Contains('*'))
    //     {
    //         var randomPart = 
    //             Path.GetRandomFileName()
    //                 .Replace(".", "")
    //                 .Substring(0, 8);
    //         name = name.Replace("*", randomPart);
    //     }
    //     
    //     var actor = typeof(T)
    //             .GetConstructor(args.Select(a => a.GetType()).ToArray())
    //             .Invoke(args)
    //         as T;
    //     
    //     var mailboxProcessor = new MailboxProcessor(name, MyMailboxProcessor, actor);
    //     actor.Self = mailboxProcessor;
    //     mailboxProcessor.Start();
    //     
    //     return actor.Self;
    // }

    public ActorBuilder WithRouter(IRoutingStrategy routingStrategy) =>
        new RouterBuilder(MyMailboxProcessor, routingStrategy);

    public virtual void AfterStart() {}
    public virtual void BeforeStop() {}
    public virtual void BeforeRestart(Exception e, object message) {}
    public virtual void AfterRestart() {}

    /// <summary>
    /// must be implemented to handle a single message async
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public abstract Task OnReceiveAsync(object message);

    /// <summary>
    /// Send a message to a receiving actor
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="message"></param>
    public void Tell(IActorRef receiver, object message) =>
        receiver.SendMessage(Self, receiver, message);

    /// <summary>
    /// Reply to sender with a given message
    /// </summary>
    /// <param name="message"></param>
    public void Reply(object message) =>
        MyMailboxProcessor.Reply(message);

    /// <summary>
    /// Forward a message to another actor keeping the sender equal
    /// </summary>
    /// <param name="receiver"></param>
    public void Forward(IActorRef receiver) =>
        MyMailboxProcessor.Forward(receiver);

    public Task<T> Ask<T>(IActorRef receiver, object message, int timeOutMillis = 500)
    {
        var answer = new TaskCompletionSource<T>();
        var askActor = ActorOf<AskActor<T>>("ask-*", receiver, message, answer, timeOutMillis);
        return answer.Task;
    }
    
    /// <summary>
    /// put the currently processed message onto Stash (typically in BeforeRestart hook)
    /// </summary>
    public void Stash() => MyMailboxProcessor.Stash();
    
    /// <summary>
    /// Clear the entire stash
    /// </summary>
    public void ClearStash() => MyMailboxProcessor.ClearStash();
    
    /// <summary>
    /// Recover all messages from stash and put them into our mailbox
    /// </summary>
    public void UnStashAll() => MyMailboxProcessor.UnStashAll();
    
    public override string ToString() => MyMailboxProcessor.ToString();
}
