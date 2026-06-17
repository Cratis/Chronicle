# Slice Anatomy — Reference

All backend artifacts for a slice go in a single `<SliceName>.cs` file. Below are complete patterns for every artifact type.

---

## File header

Every `.cs` file starts with:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```

File-scoped namespace (no extra indentation):

```csharp
namespace MyApp.Authors.Registration;
```

---

## Events

```csharp
[EventType]
public record AuthorRegistered(AuthorName Name);
```

Rules:
- `[EventType]` takes **no arguments** — the type name is the identifier
- Past tense: `ItemAddedToCart`, `UserRegistered`, `AddressChangedForPerson`
- No nullable properties — ambiguous events need a second event
- One purpose per event

---

## Commands (model-bound)

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

`Handle()` return types:

| Return | When |
| --- | --- |
| `TEvent Handle()` | Single event, no client result needed |
| `(TResult, TEvent) Handle()` | Return a value to the client + append one event |
| `Result<TSuccess, TError> Handle()` | Business rule success/failure path |
| `void Handle()` | Side-effect only, no event |

Event source resolution (in priority order):
1. Parameter marked with `[Key]`
2. Parameter whose type has implicit conversion to `EventSourceId`
3. Implement `ICanProvideEventSourceId`

DI in `Handle()`: extra parameters beyond command + read model are resolved from DI automatically.

---

## Business rules via DCB (Dynamic Consistency Boundary)

Accept a read model parameter in `Handle()` — the framework injects the current projected state:

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

---

## Validators

```csharp
public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
{
    public RegisterAuthorValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Author name is required");
    }
}
```

With DI dependencies:

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

Unique property across events:

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

Unique event type per event source (one-per-stream):

```csharp
public class UniqueUser : IConstraint
{
    public void Define(IConstraintBuilder builder) =>
        builder.Unique<UserRegistered>();
}
```

---

## Read models (model-bound, preferred)

```csharp
[ReadModel]
[FromEvent<Registration.AuthorRegistered>]
public record Author(
    [Key] AuthorId Id,
    AuthorName Name)
{
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> collection) =>
        collection.Observe();

    public static Author? ById(IMongoCollection<Author> collection, AuthorId id) =>
        collection.Find(a => a.Id == id).FirstOrDefault();
}
```

Model-bound projection attributes:

| Attribute | Purpose |
| --- | --- |
| `[Key]` | Read model primary key |
| `[FromEvent<T>]` | Auto-map from event (class-level) |
| `[SetFrom<T>]` | Explicit property mapping |
| `[AddFrom<T>]` / `[SubtractFrom<T>]` | Arithmetic |
| `[Increment<T>]` / `[Decrement<T>]` | ±1 counters |
| `[Count<T>]` | Absolute count |
| `[ChildrenFrom<T>]` | Child collection from event |
| `[Join<T>]` | Join from another event stream |
| `[RemovedWith<T>]` | Remove entry when event occurs |
| `[Passive]` | On-demand only, not actively observed |

---

## Fluent projections (for complex cases)

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

AutoMap is on by default — just call `.From<>()` directly. Use `.NoAutoMap()` then explicit `.Set()` calls when you need selective mapping.

**Projections join EVENTS, never read models.**

---

## Reducers

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

- `current` is `null` for the first event — always handle initialization
- Keep reducers pure — no side effects, no I/O
- Use `with` expressions on records for state updates

---

## Reactors

```csharp
public class StockKeeping(ICommandPipeline commandPipeline) : IReactor
{
    public async Task HandleBookReserved(BookReserved @event) =>
        await commandPipeline.Execute(new DecreaseStock(@event.Isbn));
}
```

- `IReactor` is a marker interface — method dispatch by first-parameter event type
- Method name is descriptive; `EventContext` parameter is optional
- `[OnceOnly]` — skips method during event replay

---

## Integration specs

Live under `when_<behavior>/` inside the slice folder:

```csharp
namespace MyApp.Authors.Registration.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_there_are_no_authors(context context) : Given<context>(context)
{
    public class context(ChronicleOutOfProcessFixture fixture) : given.an_http_client(fixture)
    {
        public CommandResult<Guid>? Result;
        async Task Because() =>
            Result = await Client.ExecuteCommand<RegisterAuthor, Guid>(
                "/api/authors/register", new RegisterAuthor("John Doe"));
    }

    [Fact] void should_be_successful() => Context.Result.IsSuccess.ShouldBeTrue();
}
```
