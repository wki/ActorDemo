using ActorLib.Ask;
using ActorLib.Routing;

namespace ActorLib;

/// <summary>
/// Base Class for every Actor and ActorSystem (contains user code for actor)
/// </summary>
/// <remarks>
/// Contains almost no code. Every method is more or less forwarded to MailboxProcessor
/// for getting handled.
///
/// Intention:
/// * completely hide MailboxProcessor from user code
/// * prevent actor instance from being known and used
/// </remarks>
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

    /// <summary>
    /// Actor name (unique amoung siblings)
    /// </summary>
    public string Name => MyMailboxProcessor.Name;

    /// <summary>
    /// Parent of this actor
    /// </summary>
    public IActorRef Parent => MyMailboxProcessor.Parent;

    /// <summary>
    /// List of child actors
    /// </summary>
    public IReadOnlyList<IActorRef> Children => MyMailboxProcessor.Children;

    protected Actor() {}

    /// <summary>
    /// Build a (child) actor with a name and args (must match ctor)
    /// </summary>
    /// <param name="name">unique name across all child-siblings</param>
    /// <typeparam name="T">Type of actor class</typeparam>
    /// <returns>IActorRef allowing to reference actor</returns>
    public IActorRef ActorOf<T>(string name, params object[] args)
        where T : Actor => MyMailboxProcessor.ActorOf<T>(name, args);

    /// <summary>
    /// Build a router with some actors in behind being used by some routing strategy
    /// </summary>
    /// <param name="routingStrategy"></param>
    /// <returns></returns>
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
    /// Send a message to some receiver
    /// </summary>
    /// <param name="receiver">reference to receiver of the message</param>
    /// <param name="message">message to get passed to the receiver</param>
    public void Tell(IActorRef receiver, object message) =>
        receiver.SendMessage(Self, receiver, message);

    /// <summary>
    /// Reply to sender of currently processing message with a given answer
    /// </summary>
    /// <param name="message">answer message</param>
    public void Reply(object message) => 
        MyMailboxProcessor.Reply(message);

    /// <summary>
    /// Forward a message to another actor keeping the sender equal (reply will lead to original sender)
    /// </summary>
    /// <param name="receiver">reference to receiver intended to handle the message</param>
    public void Forward(IActorRef receiver) => 
        MyMailboxProcessor.Forward(receiver);

    /// <summary>
    /// send a message to a receiver, wait asyncronously for an answer of Type T with timeout
    /// </summary>
    /// <param name="receiver">reference to receiver to be asked</param>
    /// <param name="message">message for the receiver</param>
    /// <param name="timeOutMillis">maximum duration for the answer to arrive (Otherwise task will throw exception)</param>
    /// <typeparam name="T">Type of the answer expected</typeparam>
    /// <returns></returns>
    public Task<T> Ask<T>(IActorRef receiver, object message, int timeOutMillis = 500)
    {
        var answer = new TaskCompletionSource<T>();
        var askActor = ActorOf<AskActor<T>>("ask-*", receiver, message, answer, timeOutMillis);
        return answer.Task;
    }
    
    /// <summary>
    /// put the currently processed message onto Stash (typically in BeforeRestart hook)
    /// </summary>
    public void Stash() => 
        MyMailboxProcessor.Stash();
    
    /// <summary>
    /// Clear the entire stash
    /// </summary>
    public void ClearStash() => 
        MyMailboxProcessor.ClearStash();
    
    /// <summary>
    /// Recover all messages from stash and put them into our mailbox
    /// </summary>
    public void UnStashAll() => 
        MyMailboxProcessor.UnStashAll();
    
    public override string ToString() => $"Actor '{Name}'";
}
