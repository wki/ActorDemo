# ActorDemo

A simple experiment with a minimalistic Actor System

 * runs on one process only (no remoting, no cluster)
 * actors only have one state
 * message dispatching in one method only
 * no built-in timers
 * no built-in forwarding and dispatching logic


## Example

### Build an actor system

````sharp
// build Actor System 
var system = new ActorSystem("some name");

// build a top-level actor
var someActor = system.ActorOf<ActorType>("some-name");

// send a message to an actor (sender: system)
system.Tell(someActor, new SomeMessage(...));

// do something to keep actorsystem running.
Thread.Sleep(TimeSpan.Infinite);

````

### Anatomy of an actor

````csharp
public class Initiator : Actor
{
    private readonly System.Threading.Timer _timer;
    private readonly IActorRef _receiver;

    // via ctor, we know about another actor
    public Initiator(IActorRef receiver)
    {
        _receiver = receiver;
        _timer = new Timer(TimerElapsed, new TimeOver(), TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3));
    }

    // handles a timer event by sending the other actor a message
    private void TimerElapsed(object o)
    {
        var state = (TimeOver)o;
        Tell(_receiver, state);
    }

    // handle all incoming messages in-turn        
    public override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            // we received a Ping from outside. see above
            case Ping ping:
                Console.WriteLine($"{Self} Ping received: {ping}");
                Tell(_receiver, new Pong("also hi"));
                return Task.CompletedTask;
                
            // someone told us to stop the timer.
            case StopTimer:
                Console.WriteLine($"{Self} StopTimer received");
                return _timer.DisposeAsync().AsTask();
        }

        Console.WriteLine($"{Self}: unhandled Message: {message}");
        return Task.CompletedTask;
    }
}
````
