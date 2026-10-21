```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ProjectRenamed(string Name);

public static class RenameProject
{
    // Capture this BEFORE you load the form data, then return it with the form. Load the data from the
    // event log or a passive read model (a fresh fold), never from a materialized read model.
    public static async Task<EventSequenceNumber> GetRevision(IEventLog eventLog, EventSourceId projectId)
    {
        var tail = await eventLog.GetTailSequenceNumber(projectId);

        // A project with no events has no tail (Unavailable). An explicit scope skips that value, so
        // return BeforeFirst: "no matching event had been appended when the user loaded the form".
        return tail.IsUnavailable ? EventSequenceNumber.BeforeFirst : tail;
    }

    public static async Task<bool> Rename(
        IEventLog eventLog,
        EventSourceId projectId,
        string name,
        EventSequenceNumber revisionTheUserSaw)
    {
        // Reject if anyone renamed the project after the revision the user saw.
        var scope = new ConcurrencyScopeBuilder()
            .WithEventSourceId(projectId)
            .WithSequenceNumber(revisionTheUserSaw)
            .WithEventType<ProjectRenamed>()
            .Build();

        var result = await eventLog.Append(projectId, new ProjectRenamed(name), concurrencyScope: scope);
        return result.IsSuccess;
    }
}
```
