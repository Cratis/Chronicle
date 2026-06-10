---
title: Test a slice
description: Prove a slice works — the right events appended, the right read model state — with EventScenario and ReadModelScenario running the real Chronicle engines in-process.
---

**Goal:** verify a feature actually works — the right events get appended (constraints included) and the read model derived from them comes out right — without standing up a Chronicle server or a database.

## Real engines, in-memory storage

The `Cratis.Chronicle.Testing` package gives you two scenario types that wire up the **real client and kernel code paths** — constraint validation, serialization, the projection and reducer engines. Only the storage layer is in-memory, so there's no expensive infrastructure to spin up: the specs are lightweight to run, yet exercise the same engine code that runs in production.

```bash
dotnet add package Cratis.Chronicle.Testing
```

The examples below use the [Cratis Specifications](/testing-with-cratis/) style (`Establish`/`Because`/`should_`), but the scenarios work with any test framework.

## Test the events with EventScenario

`EventScenario` exercises the appending side. Seed pre-existing history with `Given`, append through `EventLog` exactly like production code does, and assert on the `AppendResult`:

```csharp
public class when_adding_a_book : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _result;

    void Establish() => _scenario = new EventScenario();

    async Task Because() =>
        _result = await _scenario.EventLog.Append(
            BookId.New(),
            new BookAdded("The Pragmatic Programmer", "978-0135957059"));

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}
```

Constraints are discovered automatically, the same way the real client discovers them. So given a unique-ISBN constraint (like the one in [Enforce a unique value](./enforce-a-unique-value.md)), seeding a book and appending a duplicate proves the rule fires:

```csharp
public class and_the_isbn_already_exists : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _result;

    async Task Establish()
    {
        _scenario = new EventScenario();
        await _scenario.Given
            .ForEventSource(BookId.New())
            .Events(new BookAdded("The Pragmatic Programmer", "978-0135957059"));
    }

    async Task Because() =>
        _result = await _scenario.EventLog.Append(
            BookId.New(),
            new BookAdded("The Pragmatic Programmer, 2nd ed.", "978-0135957059"));

    [Fact] void should_be_rejected() => _result.ShouldBeFailed();
    [Fact] void should_report_the_violated_constraint() => _result.ShouldHaveConstraintViolationFor("UniqueIsbn");

    public void Dispose() => _scenario.Dispose();
}
```

Beyond `ShouldBeSuccessful`/`ShouldBeFailed` and `ShouldHaveConstraintViolationFor`, there are assertions for concurrency violations and errors — the full family is in [Append result assertions](../testing/events/assertions.md). Create a fresh `EventScenario` per spec and dispose it; the in-memory log accumulates state across calls on the same instance.

## Test the read model with ReadModelScenario

`ReadModelScenario<TReadModel>` exercises the deriving side: feed a history of events through the **real projection or reducer engine** and assert on the resulting instance. It auto-detects how the read model is built — reducer, fluent projection, or model-bound attributes:

```csharp
public class when_a_book_is_borrowed : Specification
{
    readonly BookId _bookId = BookId.New();
    readonly ReadModelScenario<Book> _scenario = new();

    Task Because() =>
        _scenario.Given
            .ForEventSource(_bookId)
            .Events(
                new BookAdded("The Pragmatic Programmer", "978-0135957059"),
                new BookBorrowed("Ada Lovelace"));

    [Fact] void should_be_on_loan() => _scenario.Instance!.OnLoan.ShouldBeTrue();
    [Fact] void should_record_the_borrower() => _scenario.Instance!.BorrowedBy.ShouldEqual("Ada Lovelace");
}
```

For a read model spec the event history **is** the act — `Given` supplies the input, `Instance` is the output. If the spec fails, your mapping (`[SetFrom<T>]`/`[SetValue<T>]` attributes or reducer logic) is wrong, because the engine applying it is the real one.

## What this does and doesn't cover

Together the two scenarios prove the Chronicle slice: command logic appends the right facts, and the facts fold into the right state. They don't exercise your HTTP surface or UI — that's a concern for your application framework's own testing story.

## See also

- [Testing](../testing/) — the full testing model, including `ReactorScenario` for reactor side effects.
- [EventScenario](../testing/events/scenario.md) and [ReadModelScenario](../testing/read-models/scenario.md) — every option, including initial state and dependency injection.
- [Enforce a unique value](./enforce-a-unique-value.md) — the constraint the duplicate-ISBN spec exercises.
