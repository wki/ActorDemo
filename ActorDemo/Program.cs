using System.Threading;
using ActorDemo;

var system = new ActorSystem("some name");

var receiver = system.ActorOf<Receiver>("receiver");
var initiator = system.ActorOf<Initiator>("sender", receiver);

// send a message to initiator. Origin === system.
// replies to here would be a dead letter...
system.Tell(initiator, new Ping("hello"));
Thread.Sleep(TimeSpan.FromSeconds(30));

// -----------------

// Sample messages
public record Ping(string Message);

public record Pong(string Answer);

public record StopTimer();

public class TimeOver
{
    public int Count { get; set; }
    public string Message { get; set; }

    public TimeOver(int count, string message)
    {
        Count = count;
        Message = message;
    }
}

// Example actors -- without state...
public class Initiator : Actor
{
    private readonly Timer _timer;
    private readonly IActorRef _receiver;
    private readonly TimeOver _state;

    public Initiator(IActorRef receiver)
    {
        _receiver = receiver;
        _state = new TimeOver(0, "time over");
        _timer = new Timer(TimerElapsed, _state, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3));
    }

    private void TimerElapsed(object o)
    {
        var state = (TimeOver)o;
        Tell(_receiver, state);
        state.Count++;
    }
        
    public override Task OnReceiveAsync(object message)
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
        }

        Console.WriteLine($"{Self}: unhandled Message: {message}");
        return Task.CompletedTask;
    }
}

public class Receiver : Actor
{
    public Receiver() { }

    public override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            case Pong pong:
                Console.WriteLine($"{Self} Pong received: {pong}");
                return Task.CompletedTask;
            case TimeOver timeOver:
                Console.WriteLine($"{Self} TimeOver received: {timeOver.Count}-{timeOver.Message}");
                if (timeOver.Count >= 3)
                    Tell(Sender, new StopTimer());
                return Task.CompletedTask;
        }

        Console.WriteLine($"{Self}: unhandled Message: {message}");
        return Task.CompletedTask;
    }
}
