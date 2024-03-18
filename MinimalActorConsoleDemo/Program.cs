using System.Diagnostics;
using MinimalActorLib;
using MinimalActorLib.Routing;

Console.WriteLine("ConsoleDemo");
var system = new ActorSystem();

var receiver = system.ActorOf<Receiver>();
var initiator = system.ActorOf<Initiator>(receiver);

// send a message to initiator. Origin === system.
// replies to here would be a dead letter...
system.Tell(initiator, new Ping("hello"));

// ask an actor for a result (async)
var result = await system.AskAsync<int>(initiator, new MeaningOfLife());
Console.WriteLine($"Received answer: {result}");

// instantiate a router with 5 routee-actors
var worker = new Router(new RoundRobinPool(5), typeof(Worker));
Console.WriteLine($"Worker = {worker} ({worker.GetType().Name})");

for (var i=0; i < 10; i++)
    system.Tell(worker, new Ping("hi worker"));

try
{
    var result2 = await system.AskAsync<int>(initiator, 42);
    Console.WriteLine($"Received answer2: {result2} -- should not occur");
}
catch (Exception e)
{
    Console.WriteLine(e);
}


var pingCounter = new PingCounter();
var benchmark = new Benchmark(pingCounter);

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
    protected override Task OnReceiveAsync(object message)
    {
        Console.WriteLine($"{this}: received {message} from {Sender}");
        return Task.CompletedTask;
    }
}

public class Initiator : Actor
{
    private readonly Timer _timer;
    private readonly Actor _receiver;
    private readonly TimeOver _state;

    public Initiator(Actor receiver)
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
        // Console.WriteLine($"Initiator - received: {message}");
        switch (message)
        {
            case Ping ping:
                Console.WriteLine($"{this} Ping received: {ping}");
                Tell(_receiver, new Pong("also hi"));
                break;
            case StopTimer:
                Console.WriteLine($"{this} StopTimer received");
                _timer.Dispose();
                break;
            case MeaningOfLife:
                Console.WriteLine($"{this} MeaningOfLife received, sender: {Sender}");
                Reply(42);
                break;
            case int i:
                Console.WriteLine($"{this} int {i} received, sender: {Sender}");
                break;
            default:
                Console.WriteLine($"{this}: unhandled Message: {message}, sender: {Sender}");
                break;                    
        }

        return Task.CompletedTask;
    }
}

public class Receiver : Actor
{
    protected override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            case Pong pong:
                Console.WriteLine($"{this} Pong received: {pong}");
                break;
            case TimeOver timeOver:
                Console.WriteLine($"{this} TimeOver received: {timeOver.Count}-{timeOver.Message}");
                if (timeOver.Count >= 3)
                    Tell(Sender, new StopTimer());
                break;
            default:
                Console.WriteLine($"{this}: unhandled Message: {message}");
                break;
        }

        return Task.CompletedTask;
    }
}

public class PingCounter : Actor
{
    private int _pingCounter = 0;

    protected override Task OnReceiveAsync(object message)
    {
        if (message is Ping ping)
        {
            if (++_pingCounter == 1_000_000)
            {
                Console.WriteLine("received 1.000.000 pings");
                Reply(new Pong(_pingCounter.ToString(("0"))));
            }
        }

        return Task.CompletedTask;
    }
}

public class Benchmark : Actor
{
    private Stopwatch _stopwatch;
    
    public Benchmark(Actor pingCounter)
    {
        Console.WriteLine("Benchmarking...");
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        Task.Run(() =>
        {
            var ping = new Ping("hi");
            for (var i = 0; i < 1_000_000; i++)
                Tell(pingCounter, ping);
        });
    }

    protected override Task OnReceiveAsync(object message)
    {
        if (message is Pong pong)
        {
            _stopwatch.Stop();
            Console.WriteLine($"Received {pong} after {_stopwatch.ElapsedMilliseconds:0}ms");
        }

        return Task.CompletedTask;
    }
}
