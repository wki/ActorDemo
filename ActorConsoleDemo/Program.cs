﻿using System.Diagnostics;
using ActorLib;
using ActorLib.Routing;

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


var pingCounter = system.ActorOf<PingCounter>("pingcounter");
var benchmark = system.ActorOf<Benchmark>("benchmark", pingCounter);

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

    protected override void OnReceive(object message)
    {
        Console.WriteLine($"{Self.ActorPath}: received {message} from {Sender}");
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

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case Ping ping:
                Console.WriteLine($"{Self} Ping received: {ping}");
                Tell(_receiver, new Pong("also hi"));
                break;
            case StopTimer:
                Console.WriteLine($"{Self} StopTimer received");
                _timer.Dispose();
                break;
            case MeaningOfLife:
                Console.WriteLine($"{Self} MeaningOfLife received, sender: {Sender}");
                Reply(42);
                break;
            default:
                Console.WriteLine($"{Self}: unhandled Message: {message}, sender: {Sender}");
                break;                    
        }
    }
}

public class Receiver : Actor
{
    public Receiver(Actor parent, string name): base(parent, name) { }

    protected override void OnReceive(object message)
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
    }
}

public class PingCounter : Actor
{
    private int _pingCounter;

    public PingCounter(Actor parent, string name) : base(parent, name)
    {
        _pingCounter = 0;
    }

    protected override void OnReceive(object message)
    {
        if (message is Ping ping)
        {
            if (++_pingCounter == 1_000_000)
            {
                Console.WriteLine("received 1.000.000 pings");
                Reply(new Pong(_pingCounter.ToString(("0"))));
            }
        }
    }
}

public class Benchmark : Actor
{
    private Stopwatch _stopwatch;
    
    public Benchmark(Actor parent, string name, Actor pingCounter): base(parent, name)
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

    protected override void OnReceive(object message)
    {
        if (message is Pong pong)
        {
            _stopwatch.Stop();
            Console.WriteLine($"Received {pong} after {_stopwatch.ElapsedMilliseconds:0}ms");
        }
    }
}