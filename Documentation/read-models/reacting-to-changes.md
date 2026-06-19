# Reacting to Read Model Changes

A read model reactor reacts to read model instances being added, modified or removed. It is a convenience layer on top of the [Watch APIs](./watching-read-models.md) — instead of subscribing to an observable yourself, you write a class and Chronicle dispatches changes to it by convention.

## The reactor

Implement the `IReadModelReactor` marker interface and add methods named `Added`, `Modified` or `Removed`. The method name selects the change it reacts to. The first parameter is the read model and selects which read model is watched.

```csharp
using Cratis.Chronicle.ReadModels;

public class AccountNotifier : IReadModelReactor
{
    public Task Added(Account account) => SendWelcome(account);

    public Task Modified(Account account) => SendUpdated(account);

    public Task Removed(Account account) => SendClosed(account);

    Task SendWelcome(Account account) => Task.CompletedTask;
    Task SendUpdated(Account account) => Task.CompletedTask;
    Task SendClosed(Account account) => Task.CompletedTask;
}
```

Reactors are discovered and started automatically — no registration is required. Subscriptions are tracked and cleaned up when the client is disposed.

## Signatures

Handler methods may be synchronous or asynchronous, with or without a `Task`:

```csharp
public void Added(Account account) { }
public Task Added(Account account) => Task.CompletedTask;
```

The first parameter is either a single read model or a collection of them:

```csharp
public Task Modified(IEnumerable<Account> accounts) => Task.CompletedTask;
```

## Taking dependencies

Beyond the read model, a handler method may take the `EventContext` of the event that caused the change, and any service from the service provider:

```csharp
public class AccountNotifier(INotificationService notifications) : IReadModelReactor
{
    public Task Modified(Account account, EventContext context, IAuditLog audit)
    {
        audit.Record(account.Id, context.SequenceNumber);
        return notifications.Notify(account.Id);
    }
}
```

## Returning side effects

Just like reactors, a handler method can return events to be appended. Return a single event, a collection, an `EventForEventSourceId`, or a mix. The `[EventStreamType]` and `[EventSourceType]` attributes on the reactor are honored when appending.

```csharp
public Task<AccountFlagged> Modified(Account account) =>
    Task.FromResult(new AccountFlagged(account.Id));
```

## Change types

| Method | Reacts to |
|---|---|
| `Added` | The read model instance was created for the first time. |
| `Modified` | The read model instance changed. |
| `Removed` | The read model instance was removed. |

The change type and the causing event's context are provided by the server for projection-backed read models.

## Materialized watching

Apply the `[Materialized]` attribute to watch through the materialized read model API instead of the change stream. The change type is then deduced by comparing successive materialized windows.

```csharp
[Materialized]
public class AccountProjector : IReadModelReactor
{
    public Task Added(Account account) => Task.CompletedTask;
}
```

## Guidance and limitations

- **Design for idempotency.** A reactor may be invoked more than once for the same change (for example after a reconnect). Handlers should be safe to run again.
- **Side effects are best-effort.** Returned events are appended on a fire-and-forget basis; an append failure is logged but does not pause anything (a read model reactor has no partition to fail). It also means handler ordering across rapid changes is not guaranteed.
- **Materialized fidelity is reduced.** Because the materialized API delivers full windows of already-deserialized instances, change type is inferred by comparing serialized values keyed by `id`, and the `EventContext` carries only the model key (no event sequence number).
- **Reducer-backed read models infer additions client-side.** Reducers compute changesets locally without a server-provided change type, so the first time a key is seen it is reported as `Added` and subsequent changes as `Modified`. Projection-backed read models use the precise change type and causing event context from the server.
