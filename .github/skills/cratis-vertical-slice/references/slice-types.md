# Slice Types — Reference

## The four slice types

| Type | When to use | Key artifacts |
| --- | --- | --- |
| **State Change** | User action that mutates system state | `[Command]` + `[EventType]` + optional validator/constraint |
| **State View** | Projecting events into queryable read models | `[ReadModel]` + projection/reducer + query methods |
| **Automation** | Reacting to events to make decisions or call external APIs | `IReactor` + optional local read models |
| **Translation** | Adapting events between slices or bounded contexts | `IReactor` → `ICommandPipeline.Execute()` |

Most features are built from a **State Change slice** paired with a **State View slice**.

---

## State Change slice

**When**: An action happens (user submits a form, a timer fires, an API is called) that changes state.

**Contains**:
- `[EventType]` records — the facts that occurred
- `[Command]` record with `Handle()` — validates intent and produces events
- `CommandValidator<T>` — input validation (FluentValidation, exported to TypeScript)
- `IConstraint` — server-side business rules enforced at event-append time

**Example**: `Authors/Registration/Registration.cs`

```
Registration.cs
├── AuthorRegistered (event)
├── UniqueAuthorName (constraint)
├── RegisterAuthorValidator (validator)
└── RegisterAuthor (command with Handle())
```

---

## State View slice

**When**: Data needs to be queried and displayed. Projects the event stream into a read model.

**Prefer model-bound** (`[ReadModel]` + attribute-based projection) over fluent `IProjectionFor<T>` unless the mapping is too complex for attributes.

**Contains**:
- `[ReadModel]` record — the query-optimized data shape
- Projection attributes (`[FromEvent<T>]`, `[SetFrom<T>]`, etc.) or `IProjectionFor<T>`
- Static query methods on the record (DI parameters auto-resolved)

**Example**: `Authors/Listing/Listing.cs`

```
Listing.cs
├── Author (read model record)
├── [FromEvent<AuthorRegistered>] (projection via attribute)
└── AllAuthors() static query method
```

---

## Automation slice

**When**: An event should trigger a side effect automatically — sending an email, calling an external API, making a decision.

**Contains**:
- `IReactor` implementation — dispatches on event type by first parameter
- Optional local read models for decision state

```csharp
public class WelcomeEmailSender(IEmailService email) : IReactor
{
    public async Task OnAuthorRegistered(AuthorRegistered @event, EventContext ctx) =>
        await email.SendWelcome(@event.Name);
}
```

---

## Translation slice

**When**: An event from one slice should trigger a command in another slice (event-driven integration). Keeps slices decoupled — neither knows about the other directly.

**Contains**:
- `IReactor` that listens to source events
- `ICommandPipeline.Execute()` to trigger a command in another slice

```csharp
public class StockKeeping(ICommandPipeline commandPipeline) : IReactor
{
    public async Task HandleBookReserved(BookReserved @event) =>
        await commandPipeline.Execute(new DecreaseStock(@event.Isbn));
}
```

---

## Decision guide

```
User action → State Change slice
Data display → State View slice
Automatic side effect → Automation slice
Cross-slice event reaction → Translation slice
```

A typical feature has at least one State Change + one State View. They are completely separate `.cs` files in separate sub-folders. They share events through the feature's namespace — the State View projection references the event type defined in the State Change file.
