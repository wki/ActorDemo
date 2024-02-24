using System.Threading.Channels;

namespace MinimalActorLib;

/// <summary>
/// Base class for an actor
/// </summary>
public class Actor
{
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly Channel<Envelope> _mailbox = Channel.CreateUnbounded<Envelope>();
    private readonly Task _eventLoop;
    private Envelope? _envelope;
    
    public Actor()
    {
        var token = _cancellationTokenSource.Token;
        _eventLoop = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    _envelope = await _mailbox.Reader.ReadAsync(token);
                    await OnReceive(_envelope.Sender, _envelope.Message);
                    _envelope = null;
                }
                catch (TaskCanceledException _)
                {
                    await OnStop();
                    break;
                }
                catch (Exception ex)
                {
                    if (!await OnError(_envelope?.Sender, _envelope?.Message, ex))
                        break;
                }
            }
        });
    }

    public void Stop() =>
        _cancellationTokenSource.Cancel();
    
    protected bool Tell(Actor receiver, object message) =>
        SendMessage(this, receiver, message);

    protected bool Reply(object message) =>
        SendMessage(this, _envelope.Sender, message);

    protected bool Forward(Actor receiver) =>
        SendMessage(_envelope.Sender, receiver, _envelope.Message);
    
    private bool SendMessage(Actor sender, Actor receiver, object message) =>
        receiver._mailbox.Writer.TryWrite(new Envelope(sender, message));

    protected virtual Task OnReceive(Actor sender, object message) => 
        Task.CompletedTask;

    protected virtual Task<bool> OnError(Actor? sender, object? message, Exception ex) => 
        Task.FromResult(false);

    protected virtual Task OnStop() => 
        Task.CompletedTask;
}
