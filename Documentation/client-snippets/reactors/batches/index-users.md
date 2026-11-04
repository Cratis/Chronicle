```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

[EventType]
public record UserRegistered(string Name);

[EventType]
public record UserRenamed(string NewName);

[EventType]
public record UserEmailChanged(string Email);

public record UserDocument(EventSourceId Id, string Name);

public record BulkUpsertFailure(EventSourceId EventSourceId, Exception Exception);

public record BulkUpsertThrottled(EventSourceId EventSourceId, TimeSpan RetryAfter);

public record BulkUpsertResponse(IEnumerable<BulkUpsertFailure> Failures, IEnumerable<BulkUpsertThrottled> Throttled);

public class UnknownUserEvent(object @event) : Exception($"Unknown user event: {@event}");

public interface ISearchIndex
{
    Task<BulkUpsertResponse> BulkUpsert(IEnumerable<UserDocument> documents, CancellationToken cancellationToken);
}

public interface IEmailConfirmations
{
    Task Send(EventSourceId accountId, string email);
}

public class Users(ISearchIndex index, IEmailConfirmations confirmations) : IReactor
{
    [Observes<UserRegistered>]
    [Observes<UserRenamed>]
    public async Task Index(EventBatch batch, BatchContext context)
    {
        var documents = batch.Select(e => e.Event switch
        {
            UserRegistered registered => new UserDocument(e.Context.EventSourceId, registered.Name),
            UserRenamed renamed => new UserDocument(e.Context.EventSourceId, renamed.NewName),
            _ => throw new UnknownUserEvent(e.Event)
        });

        var response = await index.BulkUpsert(documents, context.CancellationToken);

        foreach (var failure in response.Failures)
        {
            batch.Fail(failure.EventSourceId, failure.Exception);
        }

        foreach (var throttled in response.Throttled)
        {
            batch.Defer(throttled.EventSourceId, throttled.RetryAfter);
        }
    }

    public Task EmailChanged(UserEmailChanged @event, EventContext context) =>
        confirmations.Send(context.EventSourceId, @event.Email);
}
```
