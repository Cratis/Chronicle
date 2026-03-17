---
applyTo: "**/Features/**/*.*"
---

# Vertical Slice Architecture

A vertical slice contains *everything* for a single behavior: the command or query, the events it produces, the projections that build read models, the React component that renders the UI, and the specs that verify it all works. Everything lives together in one folder because everything changes together.

This is the opposite of layered architecture. In a layered project, adding "author registration" means touching `Commands/`, `Handlers/`, `Events/`, `Projections/`, `Models/`, and `Components/` — six folders across two tech stacks. In a vertical slice, it's one folder with one `.cs` file and one `.tsx` file. A developer working on a feature sees its entire scope without navigating elsewhere.

## Technical Stack

- .NET with C# 13 (ASP.NET Core)
- React + TypeScript (Vite)
- PrimeReact UI component library
- Cratis Arc for CQRS application model
- Cratis Chronicle for event sourcing
- MongoDB for read models
- Vitest + Mocha/Chai/Sinon for TypeScript specs
- xUnit + Cratis.Specifications + NSubstitute for C# specs

## Core Rules

These are non-negotiable because the frameworks rely on them for convention-based discovery and proxy generation:

- **Each vertical slice has its own folder with a single `.cs` file containing ALL backend artifacts.** This keeps cohesion high — you never wonder which file has the event or the validator. It's all in one place.
- **Commands define `Handle()` directly on the record — never create separate handler classes.** Arc discovers `Handle()` by convention; a separate handler class breaks that discovery.
- **`[EventType]` must have NO arguments — the type name is used automatically.** Adding a GUID or string argument is a mistake from other frameworks.
- Complete one slice end-to-end before starting the next. Half-finished slices create confusion and merge conflicts.
- Drop the `.Features` segment from namespaces (e.g. `MyApp.Projects.Registration` not `MyApp.Features.Projects.Registration`).

---

## Proxy Generation — Build Dependency

Commands and Queries generate TypeScript proxies at build time via `dotnet build`. This creates `.ts` files that the frontend imports (hooks, execute methods, change tracking). Until the backend compiles, **no proxy files exist** and frontend code cannot reference them.

**This is a hard sequencing constraint:**
1. Backend C# code must be written and compile successfully first.
2. `dotnet build` must complete — this generates the TypeScript proxy files.
3. Only then can frontend React components import and use the generated proxies.

**When running parallel agents or sub-agents:** backend and frontend work for the same slice **cannot** run in parallel. The backend agent must finish and `dotnet build` must succeed before the frontend agent starts. Independent slices (no shared events) can have their backends worked on in parallel, but each slice's frontend still depends on its own backend build completing first.

---

## Slice Types

| Type | Purpose | What It Contains |
|---|---|---|
| **State Change** | Mutates system state | Command + events + validators/constraints |
| **State View** | Projects events into queryable read models | Read model + projection + queries |
| **Automation** | Reacts to read models, makes decisions | Reactor + local read models |
| **Translation** | Adapts events across slices/systems | Reactor → triggers commands in own slice |

---

## Folder Structure

```
Features/
├── <Feature>/
│   ├── <Feature>.tsx              ← composition page (layout + menu)
│   ├── <Concept>.cs               ← shared concepts for this feature
│   ├── <SliceName>/
│   │   ├── <SliceName>.cs         ← ALL backend artifacts in ONE file
│   │   ├── <Component>.tsx        ← React component(s) for this slice
│   │   └── when_<behavior>/       ← integration specs (state-change slices)
│   │       ├── and_<scenario>.cs
│   │       └── ...
│   └── ...
```

**✅ CORRECT:**
```
Features/Authors/
├── Authors.tsx
├── AuthorId.cs
├── AuthorName.cs
├── Registration/
│   ├── Registration.cs            ← command + event + constraint
│   ├── AddAuthor.tsx
│   └── when_registering/
│       ├── and_there_are_no_authors.cs
│       └── and_name_already_exists.cs
└── Listing/
    ├── Listing.cs                 ← read model + projection + query
    └── Listing.tsx
```

**❌ WRONG — Never split by artifact type:**
```
Features/Authors/
├── Commands/RegisterAuthor.cs
├── Handlers/RegisterAuthorHandler.cs
├── Events/AuthorRegistered.cs
```

---

## What Goes in a Single Slice File

A single `<SliceName>.cs` file contains ALL of:

- `[Command]` records with `Handle()` method
- `CommandValidator<T>` (if needed)
- `IConstraint` implementations (if needed)
- `[EventType]` records
- `[ReadModel]` records with static query methods (State View slices)
- `IProjectionFor<T>` or model-bound projection attributes (State View slices)
- `IReducerFor<T>` implementations (if needed)
- `IReactor` implementations (Translation/Automation slices)
- Slice-specific `ConceptAs<T>` types

---

## Events

Events are **facts** — immutable records of things that have already happened. They are the write-side of the system and form a perfect audit trail. Once appended, an event is never modified or deleted; if something needs to be "undone," append a compensating event.

Immutable records decorated with `[EventType]` from `Cratis.Chronicle.Events`.

```csharp
[EventType]
public record AuthorRegistered(AuthorName Name);
```

**Rules:**
- `[EventType]` takes **no arguments** — the type name is the event identifier.
- Use past tense: `ItemAddedToCart`, `UserRegistered`, `AddressChangedForPerson`. Past tense reinforces that events describe what *happened*, not what *should happen*.
- Events are facts — never nullable properties. Nullable properties mean the event's meaning is ambiguous, forcing every observer to add logic to interpret it. If a property is genuinely optional, you need a second event.
- One purpose per event — never multipurpose events. A `UserUpdated` event with 10 nullable fields is a design smell; use `EmailChanged`, `NameChanged`, `AddressChanged` instead.

**❌ WRONG:**
```csharp
[EventType("ce956ea9-...")]              // ❌ No GUID argument
[EventType("AuthorRegistered")]          // ❌ No string argument
```

---

## Commands

Records decorated with `[Command]` from `Cratis.Arc.Commands.ModelBound`.

```csharp
[Command]
public record RegisterAuthor(AuthorName Name)
{
    public (AuthorId, AuthorRegistered) Handle()
    {
        var authorId = AuthorId.New();
        return (authorId, new(Name));
    }
}
```

**`Handle()` return types:**
- Single event: `public AuthorRegistered Handle() => new(Name);`
- Tuple (event + result): `public (AuthorId, AuthorRegistered) Handle() => ...`
- `Result<TSuccess, TError>`: `public Result<BookReserved, ValidationResult> Handle(Book book) => ...`
- `void` / `Task` for commands without events

**Event source resolution (in priority order):**
1. Parameter marked with `[Key]` from `Cratis.Chronicle.Keys`
2. Parameter whose type has implicit conversion to `EventSourceId`
3. Implement `ICanProvideEventSourceId`

**DI in Handle():** Extra parameters (beyond the command and read model arguments) are resolved from DI automatically.

---

## Business Rules via DCB (Dynamic Consistency Boundary)

Express business rules by accepting a **read model parameter** in `Handle()`. The framework injects the current projected state before invoking the method.

```csharp
[Command]
public record ReserveBook(ISBN Isbn, MemberId MemberId)
{
    public Result<BookReserved, ValidationResult> Handle(Book book)
    {
        if (book.Available <= 0)
            return ValidationResult.Error($"No available copies for {Isbn}");

        return new BookReserved(Isbn, MemberId);
    }
}
```

Throw a **custom exception** (never built-in) to signal a violation — the framework converts it to a failed `CommandResult`.

---

## Command Validation

Use `CommandValidator<T>` from `Cratis.Arc.Commands` (extends FluentValidation):

```csharp
public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
{
    public RegisterAuthorValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Author name is required");
    }
}
```

Validators with DI dependencies:
```csharp
public class MyValidator : CommandValidator<MyCommand>
{
    public MyValidator(IMyService service)
    {
        RuleFor(x => x).MustAsync(async (cmd, ct) => await service.IsValid(cmd));
    }
}
```

---

## Constraints

Server-side rules enforced by Chronicle at event-append time.

**Unique property across events:**
```csharp
public class UniqueAuthorName : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(_ => _
            .On<AuthorRegistered>(e => e.Name)
            .On<AuthorNameChanged>(e => e.Name)
            .RemovedWith<AuthorRemoved>()
            .WithMessage("Author name must be unique"));
}
```

**Unique event type per event source:**
```csharp
public class UniqueUser : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<UserRegistered>();
}
```

---

## Read Models & Projections

Read models are the *read side* of the system — purpose-built views optimized for specific queries. A single events stream can feed many projections, each building a different view of the same data. This is by design: specialized projections are easier to maintain, perform better, and never break unrelated features when one query's needs change.

### Model-Bound (Preferred — Attributes)

```csharp
[ReadModel]
[FromEvent<Registration.AuthorRegistered>]
public record Author(
    [Key] AuthorId Id,
    AuthorName Name)
{
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> collection) =>
        collection.Observe();
}
```

**Key attributes:**
| Attribute | Purpose |
|---|---|
| `[Key]` | Read model primary key |
| `[FromEvent<T>]` | Convention-based auto-mapping from event |
| `[FromEvent<T>(key: nameof(...))]` | Custom key from event property |
| `[SetFrom<T>]` | Explicit property mapping from event |
| `[AddFrom<T>]` / `[SubtractFrom<T>]` | Arithmetic operations |
| `[Increment<T>]` / `[Decrement<T>]` | ±1 counters |
| `[Count<T>]` | Absolute count |
| `[ChildrenFrom<T>]` | Child collection from event |
| `[Join<T>]` | Join data from another event stream |
| `[RemovedWith<T>]` | Remove entry when event occurs |
| `[Passive]` | On-demand projection, not actively observed |
| `[NotRewindable]` | Forward-only, cannot be replayed |

### Fluent Projection (Alternative — for complex cases)

```csharp
public class AuthorProjection : IProjectionFor<Author>
{
    public void Define(IProjectionBuilderFor<Author> builder) => builder
        .From<AuthorRegistered>();
}
```

AutoMap is **on by default** — you only need `.AutoMap()` if you previously called `.NoAutoMap()`. Most projections just call `.From<>()` directly.

**Complex projection with joins:**
```csharp
public class BorrowedBooksProjection : IProjectionFor<BorrowedBook>
{
    public void Define(IProjectionBuilderFor<BorrowedBook> builder) => builder
        .From<BookBorrowed>(from => from
            .Set(m => m.UserId).To(e => e.UserId)
            .Set(m => m.Borrowed).ToEventContextProperty(c => c.Occurred))
        .Join<BookAddedToInventory>(j => j
            .On(m => m.Id)
            .Set(m => m.Title).To(e => e.Title))
        .RemovedWith<BookReturned>();
}
```

**Projections join EVENTS, never read models.** This is fundamental to event sourcing — projections rebuild state from the event stream, not from other projections. A projection that reads from another read model creates a hidden dependency and breaks replay.

### Passive Projections

For read models used only in DCB business rules (not for queries):
```csharp
public class BookProjection : IProjectionFor<Book>
{
    public void Define(IProjectionBuilderFor<Book> builder) => builder
        .Passive()
        .From<BookAddedToInventory>(e => e.Increment(book => book.Available))
        .From<BookReserved>(e => e.Decrement(book => book.Available));
}
```

---

## Queries

Static methods on `[ReadModel]` records. Favor reactive queries (`ISubject<T>`) for real-time updates.

```csharp
public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> collection) =>
    collection.Observe();

public static Author? ById(IMongoCollection<Author> collection, AuthorId id) =>
    collection.Find(a => a.Id == id).FirstOrDefault();
```

Method parameters are DI-resolved. `IMongoCollection<T>` is automatically available for `[ReadModel]` types.

---

## Reducers

Reducers are the imperative counterpart to projections. Where projections are declarative mappings, reducers give you full control: receive the current state and an event, return the new state. Use them when mapping alone can't express the logic — aggregations, conditional updates, or complex transformations.

```csharp
public class AccountBalanceReducer : IReducerFor<AccountBalance>
{
    public AccountBalance OnDepositMade(DepositMade @event, AccountBalance? current, EventContext context)
    {
        var balance = current?.Balance ?? 0m;
        return new AccountBalance(balance + @event.Amount, context.Occurred);
    }
}
```

- `current` is `null` for the first event — always handle initialization gracefully.
- Keep reducers pure — no side effects, no database calls, no I/O. They must be safe to replay.
- Use immutable state with `with` expressions on records.

---

## Reactors

Reactors are the "if this then that" of event sourcing — they observe events and produce side effects. Unlike projections (which build state) and reducers (which transform state), reactors *do things*: send emails, trigger commands in other slices, call external APIs.

Side-effect observers for Translation and Automation slices.

```csharp
public class StockKeeping(ICommandPipeline commandPipeline) : IReactor
{
    public async Task HandleBookReserved(BookReserved @event) =>
        await commandPipeline.Execute(new DecreaseStock(@event.Isbn));
}
```

- `IReactor` is a marker interface — method dispatch is by first-parameter type.
- Method name can be anything descriptive.
- `EventContext` parameter is optional.
- `[OnceOnly]` — method executes only on first processing, skipped during replay.
- If a reactor throws, the failing event source partition pauses until resolved.

---

## Event Seeding

Provide predefined events at startup. Chronicle deduplicates automatically.

```csharp
public class EmployeeSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.ForEventSource("employee-1", [
            new PersonHired("John", "Doe", "Engineer"),
            new EmployeeAddressSet("123 Main St", "Springfield", "62701", "US")
        ]);
    }
}
```

Use `#if DEBUG` to prevent seeding in production.

---

## Concepts

- Prefer `ConceptAs<T>` over raw primitives everywhere.
- Concepts shared between slices → feature folder.
- Concepts shared between features → `Features/` root.
- One file per concept.

See [Concepts Instructions](./concepts.instructions.md) for full details.

---

## Development Workflow

Work **slice-by-slice** in this exact order. The sequence matters — TypeScript proxies are generated from C# during `dotnet build`, so frontend work cannot begin until the backend compiles.

1. **Backend** — Implement the C# slice file (command/query/projection + events, validators, constraints)
2. **Specs** — Write integration specs for state-change slices
3. **Build** — Run `dotnet build` to generate TypeScript proxies (this step creates the `.ts` files the frontend imports)
4. **Frontend** — Implement React component(s) using the auto-generated proxies
5. **Composition** — Register in the feature's composition page
6. **Routes** — Add/update routing if needed

### Sub-agent coordination

When parallel agents or sub-agents are used, steps 1–3 are a **hard prerequisite** for step 4. Backend and frontend for the same slice cannot run in parallel — the frontend agent must wait for `dotnet build` to complete. Independent slices (no shared event types) can have their backend phases worked on in parallel, but each slice's frontend still depends on its own build completing first.

---

## Integration Specs

Specs live under `when_<behavior>/` folders directly inside the slice folder — no `for_` prefix.

```csharp
using context = MyApp.Authors.Registration.when_registering.and_there_are_no_authors.context;

namespace MyApp.Authors.Registration.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_there_are_no_authors(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public CommandResult<Guid>? Result;

        async Task Because()
        {
            Result = await Client.ExecuteCommand<RegisterAuthor, Guid>(
                "/api/authors/register",
                new RegisterAuthor("John Doe"));
        }
    }

    [Fact] void should_be_successful() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_appended_only_one_event() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    [Fact] void should_append_author_registered_event() => Context.ShouldHaveAppendedEvent<AuthorRegistered>(
        EventSequenceNumber.First, Context.Result.Response,
        evt => evt.Name.Value.ShouldEqual("John Doe"));
}
```

---

## Frontend

### Listing View

```tsx
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { AllAuthors } from './AllAuthors';

export const Listing = () => {
    const [result] = AllAuthors.use();

    return (
        <DataTable value={result.data} scrollable scrollHeight="flex" emptyMessage="No authors found.">
            <Column field="name" header="Name" />
        </DataTable>
    );
};
```

### Composition Page

```tsx
import { Page } from '../../Components/Common';
import { AddAuthor } from './Registration/AddAuthor';
import { Listing } from './Listing/Listing';
import { useDialog } from '@cratis/arc.react/dialogs';
import { Menubar } from 'primereact/menubar';
import { MenuItem } from 'primereact/menuitem';
import * as mdIcons from 'react-icons/md';

export const Authors = () => {
    const [AddAuthorDialog, showAddAuthorDialog] = useDialog(AddAuthor);

    const menuItems: MenuItem[] = [
        {
            label: 'Add Author',
            icon: mdIcons.MdAdd,
            command: async () => { await showAddAuthorDialog(); }
        }
    ];

    return (
        <Page title="Authors">
            <Menubar model={menuItems} />
            <Listing />
            <AddAuthorDialog />
        </Page>
    );
};
```

### Dialogs

- **Never** import `Dialog` from `primereact/dialog` — use Cratis wrappers.
- `CommandDialog` from `@cratis/components/CommandDialog` — for dialogs that execute commands.
- `Dialog` from `@cratis/components/Dialogs` — for data collection without commands.
- Do not render manual `<Button>` for dialog actions.
