namespace ActorDemo;

public interface IActorBuilder
{
    /// <summary>
    /// Build a (child) actor
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    IActorRef ActorOf<T>(string name, params object[] args)
        where T : Actor;
}