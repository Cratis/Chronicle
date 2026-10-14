```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

[EventType]
public record NoteAdded(string Text);

// One stream per author: authors write notes on the same project without conflicting with each other.
[EventSource("Project", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream(
    "Notes",
    Description = "Notes on a project, one stream per author",
    Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class ProjectEventSource : IEventSource;

public class NoteService(IEventLog eventLog)
{
    public Task<AppendResult> AddNote(EventSourceId projectId, string authorId, string text) =>
        eventLog.Append<ProjectEventSource>(
            projectId,
            new NoteAdded(text),
            eventStream: "Notes",
            eventStreamId: authorId);
}
```
