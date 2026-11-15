```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

[EventType]
public record DecOrderingCommentPosted(Guid CommentId, string Text);

[EventType]
public record DecOrderingTaskAdded(Guid TaskId, string Title, int Priority);

public record DecOrderingComment(Guid CommentId, string Text);

public record DecOrderingTask(Guid TaskId, string Title, int Priority);

public record DecOrderingBoard(IEnumerable<DecOrderingComment> Comments, IEnumerable<DecOrderingTask> Tasks);

public class DecOrderingBoardProjection : IProjectionFor<DecOrderingBoard>
{
    public void Define(IProjectionBuilderFor<DecOrderingBoard> builder) => builder
        .Children(m => m.Comments, children => children
            .IdentifiedBy(c => c.CommentId)
            .Prepend()
            .From<DecOrderingCommentPosted>(_ => _.UsingKey(e => e.CommentId)))
        .Children(m => m.Tasks, children => children
            .IdentifiedBy(t => t.TaskId)
            .OrderByDescending(t => t.Priority)
            .From<DecOrderingTaskAdded>(_ => _.UsingKey(e => e.TaskId)));
}
```
