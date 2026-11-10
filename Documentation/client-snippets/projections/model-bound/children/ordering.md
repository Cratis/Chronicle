```csharp title="Order children"
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CommentPostedForOrdering(Guid CommentId, string Text);

[EventType]
public record TaskAddedForOrdering(Guid TaskId, string Title, int Priority);

public record OrderedComment([Key] Guid CommentId, string Text);

public record OrderedTask([Key] Guid TaskId, string Title, int Priority);

public record OrderedBoard(
    [Key] Guid Id,

    [ChildrenFrom<CommentPostedForOrdering>(key: nameof(CommentPostedForOrdering.CommentId), Prepend = true)]
    IEnumerable<OrderedComment> Comments,

    [ChildrenFrom<TaskAddedForOrdering>(key: nameof(TaskAddedForOrdering.TaskId), OrderByDescending = nameof(OrderedTask.Priority))]
    IEnumerable<OrderedTask> Tasks);
```
