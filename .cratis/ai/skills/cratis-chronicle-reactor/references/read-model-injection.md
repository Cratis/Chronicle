<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/read-model-injection.md -->
# Injecting a read model

This reference holds how a reactor read-model parameter is keyed and when it is current.

### Injecting a read model

A read-model parameter is materialized using the event context's event-source id
by default. When the model's key differs from the triggering event's source —
the event carries a related entity's id — implement `ICanResolveReadModelKey` on
the reactor:

```csharp
public class <ReactorName> : IReactor, ICanResolveReadModelKey
{
    public ReadModelKey Resolve(object @event, EventContext context) =>
        ((<EventName>)@event).<RelatedIdProperty>;

    public Task <MethodName>(<EventName> @event, <ReadModelName> <readModel>) =>
        Task.CompletedTask;
}
```

The resolved key applies to every read-model parameter on the reactor.

Materialized models are as current as their observer. `[Passive]` computes
on demand without a sink when the reactor needs a current read.

Prefer the event's own data. CHR0032 warns against injected storage primitives such as `IMongoCollection<T>`;
read keyed state through read-model parameters or `IReadModels.GetInstanceById`.
