---
applyTo: "**/for_*/**/*.cs, **/when_*/**/*.cs"
paths:
  - "**/for_*/**/*.cs"
  - "**/when_*/**/*.cs"
---

# How to Write C# Specs

Extends the base [specs.md](./specs.md) (folder structure, naming, philosophy) with C#-specific conventions. This file is the **universal foundation** used in **both** profiles — the `Cratis.Specifications` pattern (`Establish → Because → should_`) with NSubstitute.

> **Which spec surface?**
> - **Framework / library code** (Chronicle kernel, Arc pipeline, source generators, Fundamentals): the `Specification` base + NSubstitute in this file is the dominant mode — unit-test the classes under test in isolation. Reach for a `*Scenario` helper (in [specs.scenarios.csharp.md](./specs.scenarios.csharp.md)) **only** when testing the very engine your repo provides — Arc → `CommandScenario`, Chronicle → the event/projection/reactor scenarios. The **write-specs** skill is application-oriented, not for general framework specs.
> - **Event-sourced applications on Cratis** (commands, projections, reducers, reactors, constraints): use this base **plus** the in-process scenario family — see **[specs.scenarios.csharp.md](./specs.scenarios.csharp.md)** (`profile: application`) and the **write-specs** skill.

`Cratis.Specifications` keeps the approach of Machine.Specifications (MSpec): `Establish → Because → should_` maps to "Given → When → Then" and keeps each spec focused on *one setup, one action, one set of assertions*.

## Frameworks

- [xUnit](https://xunit.net/) for execution.
- [Cratis.Specifications](https://github.com/Cratis/Specifications) for BDD specification by example — the `Specification` base class discovers `Establish`/`Because`/`Destroy` by convention (no attributes).
- [NSubstitute](https://nsubstitute.github.io/) for mocking collaborators.
- Spec projects are named `<Source>.Specs`.

## BDD pattern

`Establish` sets up the world, `Because` performs the single action under test, `should_*` facts verify individual outcomes. Any method can be `async Task`.

```csharp
public class when_combining_parts : Specification
{
    object[] _parts;
    string _result;

    void Establish() => _parts = ["First", "Second", "Third"];
    void Because() => _result = KeyHelper.Combine(_parts);

    [Fact] void should_combine_all_parts() => _result.ShouldEqual("First+Second+Third");
}
```

| Method | Purpose | Notes |
|---|---|---|
| `void Establish()` | setup, before `Because()` | each class in the chain has its own, run base-first — never call `base.Establish()` |
| `void Because()` | the single action under test | only in concrete spec files, never in a reusable context |
| `[Fact] void should_*()` | one assertion per fact | use the `ShouldXxx()` extension methods |
| `void Destroy()` | teardown after each test | |

## Reusable layered contexts

Layered contexts (`all_dependencies → a_<sut> → when_*`) capture shared setup base-first: the base mocks dependencies, the next builds the system under test, each spec adds only what's unique.

```csharp
// given/all_dependencies.cs — mocks all deps
public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    void Establish() => _eventStore = Substitute.For<IEventStore>();
}

// given/a_reactor_handler.cs — builds the system under test
public class a_reactor_handler : all_dependencies
{
    protected ReactorHandler _handler;
    void Establish() => _handler = new(_eventStore);
}
```

Put the action under test only in the concrete spec's `Because()`, never in a reusable context.

## NSubstitute patterns

```csharp
_service.GetValue(Arg.Any<string>()).Returns("result");
Arg.Is<Request>(r => r.Id == expectedId)            // matcher
_service.Received(1).DoSomething(Arg.Any<string>()); // verify
_service.DidNotReceive().DoSomethingElse(Arg.Any<int>());
_handler.Handle(Arg.Any<CommandContext>()).Throws(new Exception("fail"));
```

Catch exceptions for a unit under test with `Catch.Exception`:

```csharp
async Task Because() => _error = await Catch.Exception(_sut.DoSomething);
[Fact] void should_not_fail() => _error.ShouldBeNull();
```

## Assertion extension methods

From `Cratis.Specifications`: `.ShouldEqual(expected)`, `.ShouldBeTrue()`, `.ShouldBeFalse()`, `.ShouldBeNull()`, `.ShouldNotBeNull()`, `.ShouldBeEmpty()`, `.ShouldNotBeEmpty()`, `.ShouldContain(item)`, `.ShouldNotContain(item)`, `.ShouldContainOnly(items)`, `.ShouldBeOfExactType<T>()`, `.ShouldBeGreaterThan(n)`, `.ShouldBeLessThan(n)`.

## Conventions

- Common usings come from `GlobalUsings.Specs.cs` (`Xunit`, `NSubstitute`, `Cratis.Specifications`, …) — don't duplicate them, and don't add a using for the system-under-test namespace.
- **`using` ordering (SA1210/SA1211):** non-aliased namespaces first, then a blank line, then `using <alias> = …` aliases sorted by alias name. Alias a type whose short name collides with a namespace segment (CS0118) — `using RegisterAuthorCmd = …` style, with a domain-meaningful alias, never a technical `Command`/`Event`/`Component` suffix.
- Single-statement assertion lambdas use expression-body form (RCS1021) — `[Fact] void should_x() => result.ShouldEqual(...)`, not a `{ … }` block. Don't break long `should_` lines; don't add blank lines between `should_` methods.

## What NOT to spec

Simple properties are compiler-verified. Save spec effort for business logic, coordination between dependencies, and complex transformations.

```csharp
// ❌ Do NOT spec these
public string TableName => tableName;                         // returns constructor parameter
public IEnumerable<Property> Properties => mapper.Properties; // simple delegation

// ✅ Spec complex business logic
public decimal TotalCost => Items.Sum(i => i.Cost * i.Quantity * (1 + i.TaxRate));
```

Also don't spec logging or trivial getters.

## See also

- [specs.md](./specs.md) — folder structure, naming, BDD philosophy (the base this extends).
- [specs.scenarios.csharp.md](./specs.scenarios.csharp.md) — **application profile**: the in-process scenario family (`CommandScenario` / `EventScenario` / `ReadModelScenario` / `ReactorScenario`) + out-of-process Chronicle integration, for event-sourced apps.
- [efcore.specs.md](./efcore.specs.md) — `DbContext` specs with SQLite in-memory.
- [frontend-testing.md](./frontend-testing.md) — TypeScript/React application BDD specs.
