namespace ActorDemo;

public enum ActorState
{
    Initializing,       // Actor construction
    Idle,               // waiting for mailbox messages
    Running,            // processing a message
    Faulty,             // in error processing
    Restarting,
    Stopped,
}
