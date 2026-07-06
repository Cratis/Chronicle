```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record TaggingDynamicTagsEventOccurred(string Data);

public class TaggingDynamicTagsService(IEventLog eventLog)
{
    public Task RecordProductionCritical(EventSourceId eventSourceId) =>
        eventLog.Append(eventSourceId, new TaggingDynamicTagsEventOccurred("production issue"), tags: ["production", "critical"]);

    public Task RecordDevelopmentTest(EventSourceId eventSourceId) =>
        eventLog.Append(eventSourceId, new TaggingDynamicTagsEventOccurred("test run"), tags: ["development", "testing"]);

    public Task RecordBatchMigration(EventSourceId eventSourceId) =>
        eventLog.Append(eventSourceId, new TaggingDynamicTagsEventOccurred("batch migration"), tags: ["migration", "batch-process"]);
}
```
