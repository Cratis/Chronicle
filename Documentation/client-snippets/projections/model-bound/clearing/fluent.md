```csharp title="Clear at the root, on a child and inside a nested object"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;

[EventType]
public record MbClearingFluentNoted(string Note);

[EventType]
public record MbClearingFluentNoteCleared;

[EventType]
public record MbClearingFluentSummarised(string Headline, string Note);

[EventType]
public record MbClearingFluentSummaryNoteCleared;

[EventType]
public record MbClearingFluentTaskAdded(Guid TaskId, string Title, string Note);

[EventType]
public record MbClearingFluentTaskNoteCleared(Guid TaskId);

public record MbClearingFluentSummary(string Headline, string? Note);

public record MbClearingFluentTask([Key] Guid Id, string Title, string? Note);

public record MbClearingFluentProject(
    [Key] Guid Id,
    string? Note,
    MbClearingFluentSummary? Summary,
    IReadOnlyList<MbClearingFluentTask> Tasks);

public class MbClearingFluentProjectProjection : IProjectionFor<MbClearingFluentProject>
{
    public void Define(IProjectionBuilderFor<MbClearingFluentProject> builder) => builder
        .From<MbClearingFluentNoted>(_ => _
            .Set(m => m.Note).To(e => e.Note))
        .From<MbClearingFluentNoteCleared>(_ => _
            .Clear(m => m.Note))
        .Nested(m => m.Summary, summary => summary
            .From<MbClearingFluentSummarised>(_ => _
                .Set(m => m.Headline).To(e => e.Headline)
                .Set(m => m.Note).To(e => e.Note))
            .From<MbClearingFluentSummaryNoteCleared>(_ => _
                .Clear(m => m.Note)))
        .Children(m => m.Tasks, tasks => tasks
            .IdentifiedBy(_ => _.Id)
            .From<MbClearingFluentTaskAdded>(_ => _
                .UsingKey(e => e.TaskId)
                .Set(m => m.Title).To(e => e.Title)
                .Set(m => m.Note).To(e => e.Note))
            .From<MbClearingFluentTaskNoteCleared>(_ => _
                .UsingKey(e => e.TaskId)
                .Clear(m => m.Note)));
}
```
