using ActorSimpleLib;
using ActorSimpleLib.Routing;

Console.WriteLine("ConsoleDemo");
var system = new ActorSystem("some name");

var receiver = system.ActorOf<Receiver>("receiver");
var initiator = system.ActorOf<Initiator>("initiator", receiver);

// send a message to initiator. Origin === system.
// replies to here would be a dead letter...
system.Tell(initiator, new Ping("hello"));
// await Task.Delay(TimeSpan.FromSeconds(10));

// ask an actor for a result (async)
var result = await system.Ask<int>(initiator, new MeaningOfLife());
Console.WriteLine($"Received answer: {result}");

// instantiate a router with 5 routee-actors
var worker = system
    .WithRouter(new RoundRobinPool(5))
    .ActorOf<Worker>("worker");

Console.WriteLine($"Worker = {worker} ({worker.GetType().Name})");

for (var i=0; i < 10; i++)
    system.Tell(worker, new Ping("hi worker"));

try
{
    var result2 = await system.Ask<int>(initiator, 42);
    Console.WriteLine($"Received answer2: {result2} -- should not occur");
}
catch (Exception e)
{
    Console.WriteLine(e);
}

// do something to keep actorsystem alive...
await Task.Delay(TimeSpan.FromSeconds(30));

// -----------------

// Sample messages
public record Ping(string Message);

public record Pong(string Answer);

public record MeaningOfLife();

public record StopTimer();

public record TimeOver(int Count, string Message);

// Example actors -- without state...
public class Worker : Actor
{
    public Worker(Actor parent, string name) : base(parent, name) { }

    protected override Task OnReceiveAsync(object message)
    {
        Console.WriteLine($"{Self}: received {message} from {Sender}");
        return Task.CompletedTask;
    }
}

public class Initiator : Actor
{
    private readonly Timer _timer;
    private readonly Actor _receiver;
    private readonly TimeOver _state;

    public Initiator(Actor parent, string name, Actor receiver): base(parent, name)
    {
        _receiver = receiver;
        _state = new TimeOver(0, "time over");
        _timer = new Timer(TimerElapsed, _state, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3));
    }

    private void TimerElapsed(object o)
    {
        var state = (TimeOver)o;
        
        Tell(_receiver, state);
        state = new TimeOver(state.Count + 1, state.Message);
    }

    protected override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            case Ping ping:
                Console.WriteLine($"{Self} Ping received: {ping}");
                Tell(_receiver, new Pong("also hi"));
                return Task.CompletedTask;
            case StopTimer:
                Console.WriteLine($"{Self} StopTimer received");
                return _timer.DisposeAsync().AsTask();
            case MeaningOfLife:
                Console.WriteLine($"{Self} MeaningOfLife received, sender: {Sender}");
                Reply(42);
                return Task.CompletedTask;
        }

        Console.WriteLine($"{Self}: unhandled Message: {message}, sender: {Sender}");
        return Task.CompletedTask;
    }
}

public class Receiver : Actor
{
    public Receiver(Actor parent, string name): base(parent, name) { }

    protected override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            case Pong pong:
                Console.WriteLine($"{Self} Pong received: {pong}");
                break;
            case TimeOver timeOver:
                Console.WriteLine($"{Self} TimeOver received: {timeOver.Count}-{timeOver.Message}");
                if (timeOver.Count >= 3)
                    Tell(Sender, new StopTimer());
                break;
            default:
                Console.WriteLine($"{Self}: unhandled Message: {message}");
                break;
        }

        return Task.CompletedTask;
    }
}
