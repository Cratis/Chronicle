---
name: cratis-readmodel
description: Step-by-step guidance for creating a Cratis Chronicle read model from scratch — defining events, choosing between projection and reducer, [ReadModel] record with static query methods, and the generated TypeScript proxy in React. Use when creating a read model, working with [EventType], [ReadModel], IProjectionFor, IReducerFor, observable queries, or deriving state from events. For adding a projection or reactor to an existing read model, use add-projection instead.
---

# Creating a Cratis Read Model

A read model is derived state built from events. The path is:

```
[EventType] records  →  [ReadModel] record + static query methods  →  projection or reducer  →  TypeScript proxy  →  React
```

---

## Step 1 — Define your events

Events are the source of truth. Define each as a `record` decorated with `[EventType]`. Name them in **past tense**.

```csharp
// Domain/Events/AccountEvents.cs
using Cratis.Chronicle.Events;

[EventType]
public record DebitAccountOpened(string Name, Guid OwnerId);

[EventType]
public record DebitAccountClosed();

[EventType]
public record FundsDeposited(decimal Amount);

[EventType]
public record FundsWithdrawn(decimal Amount);
```

Good event design:
- One clear purpose per event — do not mix concerns
- No nullables unless genuinely optional
- Properties represent facts, not intent

---

## Step 2 — Define the read model record

Decorate the record with `[ReadModel]` and add **static query methods** directly on it. The proxy generator turns each static method into a TypeScript query class.

```csharp
// Domain/ReadModels/AccountSummary.cs
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

[ReadModel]
public record AccountSummary(AccountId Id, string Name, OwnerId OwnerId, decimal Balance, bool IsClosed)
{
    // Snapshot query — returns current data once
    public static async Task<IEnumerable<AccountSummary>> AllAccounts(
        IMongoCollection<AccountSummary> collection)
        => await collection.Find(Builders<AccountSummary>.Filter.Empty).ToListAsync();

    public static async Task<AccountSummary?> GetAccount(
        AccountId id,
        IMongoCollection<AccountSummary> collection)
        => await collection.Find(a => a.Id == id).FirstOrDefaultAsync();

    // Observable query — pushes updates in real time
    public static ISubject<IEnumerable<AccountSummary>> ObserveAllAccounts(
        IMongoCollection<AccountSummary> collection)
        => collection.Observe();
}
```

**Rules:**
- `[ReadModel]` attribute is **required** for proxy generation and runtime routing
- Static methods must be `public static` and return the record type, a collection of it, or `ISubject<T>` for real-time push
- Do **not** return `Task<ISubject<T>>` — observable methods must return `ISubject<T>` directly
- Use `ConceptAs<T>` wrappers for all identity fields — never raw `Guid`
- One read model per use case — do not reuse them

---

## Step 3 — Choose: projection or reducer?

| | Projection | Reducer |
|-| ---------- | ------- |
| **Best for** | Shaped read models with mapping logic, joins, children | Running aggregates: balances, counts, sums |
| **How it works** | Declarative mapping: each event updates specific fields | Receives events one by one and returns the new full state |
| **When to pick** | The read model shape comes mostly from mapping event fields | The state is a function of *accumulating* multiple events |

For the `AccountSummary` above: use a **projection** for name/owner fields and a **reducer** for balance (a running total). In practice, reducers cover both when the aggregate combines both concerns.

---

## Step 4A — Implement a projection

```csharp
// Domain/Projections/AccountSummaryProjection.cs
using Cratis.Chronicle.Projections;

public class AccountSummaryProjection : IProjectionFor<AccountSummary>
{
    public void Define(IProjectionBuilderFor<AccountSummary> builder) => builder
        .From<DebitAccountOpened>(from => from
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.OwnerId).To(e => e.OwnerId)
            .Set(m => m.Balance).WithValue(0m))
        .From<FundsDeposited>(from => from
            .Add(m => m.Balance).With(e => e.Amount))
        .From<FundsWithdrawn>(from => from
            .Subtract(m => m.Balance).With(e => e.Amount))
        .From<DebitAccountClosed>(from => from
            .Set(m => m.IsClosed).WithValue(true));
}
```

- Discovered automatically — no registration needed
- `IProjectionFor<T>` is keyed by **event source ID** by default (the `Id` passed to `IEventLog.Append`)
- Appended `tags`, `eventSourceType`, and `eventStreamType` do not filter projections directly; use reducers or reactors alongside the projection when you need metadata-based filtering
- See `references/projections.md` for joins, auto-mapping, children, composite keys

### Model-bound shorthand (for simple cases)

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record AccountInfo(
    [Key] Guid Id,
    [FromEvent<DebitAccountOpened>] string Name,   // auto-maps matching property
    [SetFrom<DebitAccountOpened>(nameof(DebitAccountOpened.OwnerId))] Guid OwnerId
);
```

---

## Step 4B — Implement a reducer

Use a reducer when the state is built by accumulating values across events:

```csharp
// Domain/Reducers/AccountBalanceReducer.cs
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

public class AccountBalanceReducer : IReducerFor<AccountBalance>
{
    public AccountBalance Opened(DebitAccountOpened @event, AccountBalance? current, EventContext context)
        => new(0m, context.Occurred);

    public AccountBalance Deposited(FundsDeposited @event, AccountBalance? current, EventContext context)
        => (current ?? new(0m, context.Occurred)) with { Balance = (current?.Balance ?? 0m) + @event.Amount };

    public AccountBalance Withdrawn(FundsWithdrawn @event, AccountBalance? current, EventContext context)
        => (current ?? new(0m, context.Occurred)) with { Balance = (current?.Balance ?? 0m) - @event.Amount };
}

public record AccountBalance(decimal Balance, DateTimeOffset LastUpdated);
```

- Return the **complete new state** — do not mutate `current`
- `current` is `null` on the first event for a given event source
- `EventContext` provides `Occurred`, `EventSourceId`, `SequenceNumber`, `CorrelationId`
- Discovered automatically — no registration needed
- Add `[FilterEventsByTag]`, `[EventSourceType]`, and `[EventStreamType]` when the reducer should only observe events appended with matching metadata

---

## Step 5 — Expose read model queries

Query methods live **directly on the `[ReadModel]` record** as static methods (see Step 2). You do **not** need a separate controller or `IReadModels` injection.

The method name becomes the TypeScript proxy class name — use descriptive names like `AllAccounts`, `GetAccount`, `ObserveAllAccounts`.

### Snapshot (one-time) queries

```csharp
[ReadModel]
public record AccountSummary(AccountId Id, string Name, decimal Balance)
{
    public static async Task<IEnumerable<AccountSummary>> AllAccounts(
        IMongoCollection<AccountSummary> collection)
        => await collection.Find(_ => true).ToListAsync();

    public static async Task<AccountSummary?> GetAccount(
        AccountId id,
        IMongoCollection<AccountSummary> collection)
        => await collection.Find(a => a.Id == id).FirstOrDefaultAsync();
}
```

### Observable (real-time push) queries

Return `ISubject<T>` to push updates as projection changes land:

```csharp
[ReadModel]
public record AccountSummary(AccountId Id, string Name, decimal Balance)
{
    public static ISubject<IEnumerable<AccountSummary>> ObserveAllAccounts(
        IMongoCollection<AccountSummary> collection)
        => collection.Observe();

    public static ISubject<AccountSummary> ObserveAccount(
        AccountId id,
        IMongoCollection<AccountSummary> collection)
        => collection.Observe(a => a.Id == id);
}
```

When the frontend uses an observable query, the query proxy type changes from `QueryFor` to `ObservableQueryFor`, and `DataPage` uses the `observableQuery` prop instead of `query`.

---

## Step 6 — Build and use in React

```bash
dotnet build   # generates TypeScript proxies
```

```tsx
import { AllAccounts } from '../api/Accounts/AllAccounts';

export const AccountList = () => {
    const [accounts] = AllAccounts.use();

    if (accounts.isPerforming) return <Spinner />;

    return (
        <ul>
            {accounts.data.map(a => (
                <li key={a.id}>{a.name} — ${a.balance}</li>
            ))}
        </ul>
    );
};
```

For building full pages with filtering, sorting, and command actions — see the `cratis-react-page` skill.

---

## Reference files

| File | What's in it |
| ---- | ------------ |
| `references/projections.md` | Full builder API: `Set`, `Add`, `Join`, `Children`, `AutoMap`, composite keys |
| `references/reducers.md` | Reducer signatures, async, passive, snapshot behaviour |
| `references/events.md` | `[EventType]`, appending, `AppendResult`, tags, constraints |
| `references/queries.md` | Query result shape, observable queries, paging |
