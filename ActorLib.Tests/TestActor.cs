using System.Text.RegularExpressions;

namespace ActorLib.Tests;

public class TestActor : Actor
{
    private bool shouldDie = true;
    
    public List<string> ReceivedMessages = new List<string>();
    public List<string> CalledHooks = new List<string>();
    
    public override Task OnReceiveAsync(object message)
    {
        var sender = Regex.Replace(Sender.ToString(), "^.*'(.+)'$", "$1");
        ReceivedMessages.Add($"{sender}:{message.GetType().Name}");

        if (shouldDie && message == "die")
            throw new ArgumentException("died just for fun");

        if (message == "alwaysdie")
            throw new ArgumentException("died just for fun");
        
        return Task.CompletedTask;
    }

    public override void AfterStart() => CalledHooks.Add("AfterStart");
    public override void BeforeRestart(Exception e, object m)
    {
        shouldDie = false;
        Stash();
        CalledHooks.Add($"BeforeRestart: {e.GetType().Name}");
    }

    public override void AfterRestart()
    {
        UnStashAll();
        CalledHooks.Add("AfterRestart");
    }

    public override void BeforeStop() => CalledHooks.Add("BeforeStop");
}