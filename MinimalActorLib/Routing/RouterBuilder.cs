namespace MinimalActorLib.Routing;

public class RouterBuilder
{
    private readonly Actor _parent;
    private readonly Type _strategyType;
    private readonly object[] _strategyArgs;

    public RouterBuilder(Actor parent, Type strategyType, object[] strategyArgs)
    {
        _parent = parent;
        _strategyType = strategyType;
        _strategyArgs = strategyArgs;
    }

    public Actor ActorOf<T>(params object[] workerCtorArgs) where T : Actor
    {
        // build routing strategy
        var strategyArgs = _strategyArgs
            .Append(typeof(T))
            .Append(workerCtorArgs)
            .ToArray();
        var strategyArgTypes = strategyArgs.Select(a => a.GetType()).ToArray();
        var strategyCtor = _strategyType.GetConstructor(strategyArgTypes)
            ?? throw new ArgumentException($"No ctor in class {_strategyType.Name} found for provided arguments ({string.Join(", ", strategyArgTypes.Select(t => t.Name))})");
        var strategy = strategyCtor.Invoke(strategyArgs) as IRoutingStrategy
            ?? throw new InvalidCastException($"Could not cast {_strategyType.Name} to IRoutingStrategy");
        
        // build router with strategy
        return _parent.ActorOf<Router>(strategy);
    }
}