﻿using System.Threading.Channels;
using MinimalActorLib.Routing;

namespace MinimalActorLib;

/// <summary>
/// Base class for Actors and our ActorSystem
/// </summary>
public class Actor
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Channel<Envelope> _mailbox = Channel.CreateUnbounded<Envelope>();
    protected Actor? Parent { get; private set; }
    protected Actor? Sender { get; private set; }
    private Timer? _timer;
    private object? _message;
    private Task? _eventLoop;
    protected int ReceiveTimeoutMs { get; set; }
    public ActorStatus ActorStatus { get; private set; } = ActorStatus.Initializing; 

    internal async Task EventLoop()
    {
        await OnStartAsync().IgnoreExceptions();

        while (true)
        {
            Sender = null;
            ActorStatus = ActorStatus.Idle;
            ActivateTimer();

            try
            {
                (Sender, _message) = await _mailbox.Reader.ReadAsync(_cancellationTokenSource.Token);
                ActorStatus = ActorStatus.Processing;
                await OnReceiveAsync(_message);
            }
            catch (TaskCanceledException)
            {
                // will be raised when Stop() is called -> halt this Actor
                break;
            }
            catch (Exception ex)
            {
                // exception in user code occured and must be handled
                ActorStatus = ActorStatus.ErrorHandling;
                if (!await OnErrorAsync(_message, ex).IgnoreExceptions())
                    break;

                ActorStatus = ActorStatus.Restarting;
                await OnRestartAsync().IgnoreExceptions();
            }
        }

        _mailbox.Writer.Complete();
        _timer?.Dispose();
        _timer = null;

        var terminatedMessage = new Terminated(Child: this, HasDied: ActorStatus == ActorStatus.ErrorHandling);
        ActorStatus = ActorStatus.Stopping;
        await OnStopAsync().IgnoreExceptions();
        ActorStatus = ActorStatus.Stopped;
        Tell(Parent, terminatedMessage);
        
        Parent = null;
        Sender = null;
        _message = null;
        _cancellationTokenSource.Dispose();
        _eventLoop?.Dispose();
        _eventLoop = null;
    }

    private void ActivateTimer()
    {
        void SendTimeOut(object? state)
        {
            if (ActorStatus == ActorStatus.Idle)
                Tell(this, TimeOut.Instance);
        }
        
        if (_timer is null)
        {
            if (ReceiveTimeoutMs > 0)
                _timer = new Timer(callback: SendTimeOut, state: null, dueTime: ReceiveTimeoutMs, period: -1); 
        }
        else if (ReceiveTimeoutMs <= 0)
        {
            _timer.Dispose();
            _timer = null;
        }
        else
            _timer.Change(dueTime: ReceiveTimeoutMs, period: -1);    
    }
    
    public void Stop() => _cancellationTokenSource.Cancel();
    
    public Actor ActorOf<T>(params object[] ctorArgs) where T : Actor => ActorOf(typeof(T), ctorArgs);

    internal Actor ActorOf(Type actorType, params object[] ctorArgs)
    {
        var ctorArgTypes = ctorArgs.Select(a => a.GetType()).ToArray();
        var ctor = actorType.GetConstructor(ctorArgTypes)
            ?? throw new ArgumentException($"No ctor in class {actorType.Name} found for provided arguments ({string.Join(", ", ctorArgTypes.Select(t => t.Name))})");
        var actor = ctor.Invoke(ctorArgs.ToArray()) as Actor
            ?? throw new InvalidCastException($"class {actorType.Name} is not derived from Actor");
        actor.Parent = this;
        actor._eventLoop = actor.EventLoop();
        return actor;
    }

    public RouterBuilder WithRouter<TStrategy>(params object[] strategyArgs) where TStrategy : IRoutingStrategy => 
        new(this, typeof(TStrategy), strategyArgs);
    
    public bool Tell(Actor? receiver, object message) => SendMessage(this, receiver, message);

    public Task<T> AskAsync<T>(Actor receiver, object question, int timeOutMs = 500)
    {
        var answer = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        ActorOf<AskActor<T>>(receiver, question, answer, timeOutMs);
        return answer.Task;
    }
    
    protected bool Reply(object message) => SendMessage(this, Sender, message);

    protected bool Forward(Actor? receiver) => SendMessage(Sender!, receiver, _message!);
    
    private static bool SendMessage(Actor sender, Actor? receiver, object message) =>
        receiver is not null
        && receiver.ActorStatus < ActorStatus.Stopping
        && receiver._mailbox.Writer.TryWrite(new Envelope(sender, message));
    
    protected virtual Task OnStartAsync() => Task.CompletedTask;
    
    protected virtual Task OnReceiveAsync(object message) => Task.CompletedTask;

    protected virtual Task<bool> OnErrorAsync(object? message, Exception ex) => Task.FromResult(false);

    protected virtual Task OnRestartAsync() => Task.CompletedTask;
    
    protected virtual Task OnStopAsync() => Task.CompletedTask;
    
    public override string ToString() => $"[Actor {GetType().Name} {GetHashCode()}]";
}
