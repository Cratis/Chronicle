```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record ProjectRenamed(string Name);

public static class RenameProject
{
    // Return this with the form. It is the last event the user could have seen for the project.
    public static Task<EventSequenceNumber> GetRevision(IEventLog eventLog, EventSourceId projectId) =>
        eventLog.GetTailSequenceNumber(projectId);

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
