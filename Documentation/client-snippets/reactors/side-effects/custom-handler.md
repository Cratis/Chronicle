```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;

public record MySpecialResult;

public class MyHandler : IReactorSideEffectHandler
{
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        value is MySpecialResult;

    public bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
        CanHandle(reactorContext, value);

    public Task<Result<ReactorSideEffectFailure>> Handle(
        ReactorContext reactorContext,
        IEventStore eventStore,
        object value)
    {
        return Task.FromResult(Result.Success<ReactorSideEffectFailure>());
    }
}

public static class ReactorSideEffectRegistration
{
    public static void Add(IServiceCollection services)
    {
        services.AddSingleton<IReactorSideEffectHandler, MyHandler>();
    }
}
```
