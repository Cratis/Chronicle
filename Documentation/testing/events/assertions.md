---
uid: Chronicle.Testing.Events.Assertions
---
# Append Assertions

Every `Append` and `AppendMany` call returns an `IAppendResult`. The `Cratis.Chronicle.Testing` package provides built-in `Should*` extension methods for asserting on these results.

## Assertion reference

| Method | Asserts |
|--------|---------|
| `result.ShouldBeSuccessful()` | Operation succeeded with no violations or errors |
| `result.ShouldBeFailed()` | Operation failed (any violation or error) |
| `result.ShouldHaveConstraintViolations()` | At least one constraint violation is present |
| `result.ShouldNotHaveConstraintViolations()` | No constraint violations are present |
| `result.ShouldHaveConstraintViolationFor(name)` | A violation for the named constraint is present |
| `result.ShouldHaveConcurrencyViolations()` | At least one concurrency violation is present |
| `result.ShouldNotHaveConcurrencyViolations()` | No concurrency violations are present |
| `result.ShouldHaveErrors()` | At least one error is present |
| `result.ShouldNotHaveErrors()` | No errors are present |

All `Should*` methods throw `AppendResultAssertionException` on failure. The exception message describes all violations and errors found, making it easy to diagnose failures.

## Happy path assertion

```csharp
var result = await scenario.EventLog.Append(authorId, new AuthorRegistered("Jane Smith"));
result.ShouldBeSuccessful();
```

## Failure assertions

```csharp
var result = await scenario.EventLog.Append(newAuthorId, new AuthorRegistered("Jane Smith"));
result.ShouldBeFailed();
```

## Constraint violation assertions

Assert that a specific named constraint fired:

```csharp
result.ShouldHaveConstraintViolations();
result.ShouldHaveConstraintViolationFor("UniqueAuthorName");
```

Assert that no constraint violations occurred:

```csharp
result.ShouldNotHaveConstraintViolations();
```

## Concurrency violation assertions

```csharp
result.ShouldHaveConcurrencyViolations();
result.ShouldNotHaveConcurrencyViolations();
```

## Error assertions

```csharp
result.ShouldHaveErrors();
result.ShouldNotHaveErrors();
```

For asserting on the event sequence itself (tail sequence number, event types, and event content), see [Event Sequence Assertions](event-sequence-assertions.md).

## Example: testing a constraint violation end-to-end

Pre-seed state with `Given`, then verify the conflicting append is rejected with the expected constraint name:

```csharp
var scenario = new EventScenario();

// Pre-seed: an author with this name is already registered
await scenario.Given
    .ForEventSource(existingAuthorId)
    .Events(new AuthorRegistered("John Doe"));

// Act: attempt to register the same name under a new event source
var result = await scenario.EventLog.Append(newAuthorId, new AuthorRegistered("John Doe"));

result.ShouldBeFailed();
result.ShouldHaveConstraintViolations();
result.ShouldHaveConstraintViolationFor("UniqueAuthorName");
```
