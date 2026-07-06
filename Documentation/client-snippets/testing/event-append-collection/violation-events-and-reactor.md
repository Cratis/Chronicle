```csharp
namespace Cratis.Chronicle.Docs.EventAppendCollection
{
    [EventType]
    public record UniqueValueRecorded(string UniqueValue);

    [EventType]
    public record UniqueValueFollowUp(string UniqueValue);

    public class UniqueValueFollowUpConstraint : IConstraint
    {
        public void Define(IConstraintBuilder builder) =>
            builder.Unique(b => b.On<UniqueValueFollowUp>(e => e.UniqueValue));
    }

    public class UniqueValueReactor(IEventLog eventLog) : IReactor
    {
        public Task OnUniqueValueRecorded(UniqueValueRecorded evt, EventContext ctx) =>
            eventLog.Append(ctx.EventSourceId, new UniqueValueFollowUp(evt.UniqueValue));
    }
}
```
