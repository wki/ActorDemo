namespace ActorDemo;

/// <summary>
/// Signature of the ActorOf<T>() method being used at various places
/// </summary>
public interface IActorBuilder
{
    /// <summary>
    /// Build a (child) actor
    /// </summary>
    /// <param name="name">name of the actor ("*" will be replaced with a random string)</param>
    /// <typeparam name="T">Class of the Actor to be instantiated</typeparam>
    /// <returns></returns>
    IActorRef ActorOf<T>(string name, params object[] args)
        where T : Actor;
}