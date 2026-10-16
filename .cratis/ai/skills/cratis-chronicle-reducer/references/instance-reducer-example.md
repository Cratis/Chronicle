<!-- cratis-ai-managed: skills/cratis-chronicle-reducer/references/instance-reducer-example.md -->
# Instance reducer example

This reference holds a worked `IReducerFor<T>` instance reducer.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

// This fold lives separately because the model is a shared data contract.
public class <ReadModelName>Reducer : IReducerFor<<ReadModelName>>
{
    public <ReadModelName> <Opened>(<OpenedEvent> @event, <ReadModelName>? current, EventContext context) =>
        new(<initialValue>, context.Occurred);

    public <ReadModelName> <Changed>(<ChangedEvent> @event, <ReadModelName>? current) =>
        current is null
            ? new(<initialValue>, default)
            : current with { <Property> = <computedFrom(current, @event)> };
}
```
