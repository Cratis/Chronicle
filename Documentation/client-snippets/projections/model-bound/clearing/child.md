```csharp title="Clear a member of a child item"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record MbClearingTaskListStarted(string Name);

[EventType]
public record MbClearingTaskAdded(Guid ListId, Guid TaskId, string Title, string Due);

[EventType]
public record MbClearingTaskDeferred(Guid ListId, Guid TaskId);

public record MbClearingTask(
    [Key] Guid Id,
    [SetFrom<MbClearingTaskAdded>(nameof(MbClearingTaskAdded.Title))] string Title,
    [SetFrom<MbClearingTaskAdded>(nameof(MbClearingTaskAdded.Due))]
    [ClearWith<MbClearingTaskDeferred>]
    string? Due);

[FromEvent<MbClearingTaskListStarted>]
public record MbClearingTaskList(
    [Key] Guid Id,

    [ChildrenFrom<MbClearingTaskAdded>(key: nameof(MbClearingTaskAdded.TaskId), parentKey: nameof(MbClearingTaskAdded.ListId), identifiedBy: nameof(MbClearingTask.Id))]
    [ChildrenFrom<MbClearingTaskDeferred>(key: nameof(MbClearingTaskDeferred.TaskId), parentKey: nameof(MbClearingTaskDeferred.ListId), identifiedBy: nameof(MbClearingTask.Id))]
    IReadOnlyList<MbClearingTask> Tasks);
```
