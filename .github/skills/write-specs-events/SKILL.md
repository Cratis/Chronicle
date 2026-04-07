---
name: write-specs-events
description: Use this skill when asked to write tests or specs for event appending, event log behavior, constraint violations, or concurrency violations using EventScenario in a Cratis-based project. Produces infrastructure-free in-process specs using EventScenario and AppendResult Should* extensions.
---
---

# Writing Event Sequence Specs with EventScenario

Use `EventScenario` to test event-appending behavior in-process — no Chronicle server, database, or network required.

## When to use this skill

- Testing that a command appends the correct event(s)
- Testing that a constraint correctly rejects a duplicate or conflicting append
- Testing `AppendMany` for batch event appends
- Testing concurrency or error conditions in event appending

For testing **read model projections or reducers**, use `ReadModelScenario<TReadModel>` instead.

---

## Package

```bash
dotnet add package Cratis.Chronicle.Testing
```

---

## Basic structure

```csharp
// In your spec file (follows BDD Establish/Because/should_ pattern)
public class when_appending_<event_name> : Specification
{
    EventScenario _scenario;
    IAppendResult _result;

    void Establish() => _scenario = new EventScenario();

    async Task Because() =>
        _result = await _scenario.EventLog.Append(<eventSourceId>, new <EventType>(<args>));

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();
}
```

---

## Step 1 — Create the scenario

Always create a **new** `EventScenario` per test. It starts with an empty event log and sequence number zero.

```csharp
var scenario = new EventScenario();
```

---

## Step 2 — Seed pre-existing state (optional)

Use the fluent `Given` builder when the act phase depends on prior state:

```csharp
await scenario.Given
    .ForEventSource(authorId)
    .Events(new AuthorRegistered("Jane Smith"), new BookAdded("Clean Code"));
```

Chain multiple `ForEventSource` calls to seed different event sources:

```csharp
await scenario.Given
    .ForEventSource(author1Id)
    .Events(new AuthorRegistered("Jane Smith"));

await scenario.Given
    .ForEventSource(author2Id)
    .Events(new AuthorRegistered("John Doe"));
```

**Rules:**
- Call `Given` **before** the act phase — seeded events get monotonically increasing sequence numbers.
- Do not put the act-under-test inside `Given`; only pre-existing state goes here.

---

## Step 3 — Append and assert

```csharp
var result = await scenario.EventLog.Append(eventSourceId, new SomeEvent("value"));
result.ShouldBeSuccessful();
```

Use `AppendMany` for batch appends:

```csharp
var result = await scenario.EventLog.AppendMany(cartId, [
    new ItemAddedToCart(itemId1),
    new ItemAddedToCart(itemId2)
]);
result.ShouldBeSuccessful();
```

---

## Assertion reference

| Method | Asserts |
|--------|---------|
| `result.ShouldBeSuccessful()` | Operation succeeded with no violations or errors |
| `result.ShouldBeFailed()` | Operation failed (any violation or error) |
| `result.ShouldHaveConstraintViolations()` | At least one constraint violation is present |
| `result.ShouldNotHaveConstraintViolations()` | No constraint violations are present |
| `result.ShouldHaveConstraintViolationFor("ConstraintName")` | A violation for the named constraint is present |
| `result.ShouldHaveConcurrencyViolations()` | At least one concurrency violation is present |
| `result.ShouldNotHaveConcurrencyViolations()` | No concurrency violations are present |
| `result.ShouldHaveErrors()` | At least one error is present |
| `result.ShouldNotHaveErrors()` | No errors are present |

All `Should*` methods throw `AppendResultAssertionException` on failure with a message that lists all violations and errors found.

---

## Example: happy path

```csharp
public class when_registering_an_author : Specification
{
    EventScenario _scenario;
    IAppendResult _result;

    void Establish() => _scenario = new EventScenario();

    async Task Because() =>
        _result = await _scenario.EventLog.Append(AuthorId.New(), new AuthorRegistered("Jane Smith"));

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();
    [Fact] void should_not_have_constraint_violations() => _result.ShouldNotHaveConstraintViolations();
}
```

---

## Example: constraint violation (duplicate)

Seed the pre-existing state, then verify the second append is rejected:

```csharp
public class when_registering_an_author
{
    public class and_the_name_already_exists : Specification
    {
        EventScenario _scenario;
        IAppendResult _result;

        async Task Establish()
        {
            _scenario = new EventScenario();
            await _scenario.Given
                .ForEventSource(AuthorId.New())
                .Events(new AuthorRegistered("Jane Smith"));
        }

        async Task Because() =>
            _result = await _scenario.EventLog.Append(AuthorId.New(), new AuthorRegistered("Jane Smith"));

        [Fact] void should_be_failed() => _result.ShouldBeFailed();
        [Fact] void should_have_a_constraint_violation() => _result.ShouldHaveConstraintViolations();
        [Fact] void should_have_constraint_violation_for_unique_name() =>
            _result.ShouldHaveConstraintViolationFor("UniqueAuthorName");
    }
}
```

---

## Example: batch append

```csharp
public class when_adding_multiple_items_to_cart : Specification
{
    EventScenario _scenario;
    IAppendResult _result;

    void Establish() => _scenario = new EventScenario();

    async Task Because() =>
        _result = await _scenario.EventLog.AppendMany(cartId, [
            new ItemAddedToCart(itemId1),
            new ItemAddedToCart(itemId2),
            new ItemQuantityAdjusted(itemId1, 3)
        ]);

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();
}
```

---

## Checklist

- [ ] New `EventScenario` instance per test method (never shared state)
- [ ] `Given` called before the act phase for all pre-existing state
- [ ] Each test asserts a single outcome (one `should_` per behavior)
- [ ] Constraint violation tests include `ShouldHaveConstraintViolationFor("ConstraintName")`
- [ ] Run `dotnet build` and `dotnet test` — fix all failures before completing
