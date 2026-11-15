```csharp
using Cratis.Chronicle;

public class EnrichersTenantRegionTagEnricher(IRegionLookup regions) : IEventTagEnricher
{
    public IEnumerable<NamedTag> GetTags(EventTagContext context)
    {
        if (context.EventSequenceId != Cratis.Chronicle.EventSequences.EventSequenceId.Outbox)
        {
            return [];
        }

        return [new NamedTag(new TagName("region"), regions.For(context.EventSourceId.Value))];
    }
}

public interface IRegionLookup
{
    string For(string eventSourceId);
}
```
