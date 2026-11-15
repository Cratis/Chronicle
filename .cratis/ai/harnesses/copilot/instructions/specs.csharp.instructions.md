---
applyTo: "**/for_*/**/*.cs, **/when_*/**/*.cs"
paths:
  - "**/for_*/**/*.cs"
  - "**/when_*/**/*.cs"
---
<!-- cratis-ai-managed: harnesses/copilot/instructions/specs.csharp.instructions.md -->
<!-- cratis-ai: generated Copilot instructions. Do not edit; rules/ is the source. -->

# How to Write C# Specs

Extends the base [specs.md](./specs.instructions.md) (folder structure, naming, philosophy) with C#-specific conventions. This file is the **universal foundation** used in **both** profiles — the `Cratis.Specifications` pattern (`Establish → Because → should_`) with NSubstitute.

> **Which spec surface?**
> - **Framework / library code** (Chronicle kernel, Arc pipeline, source generators, Fundamentals): the `Specification` base + NSubstitute in this file is the dominant mode — unit-test the classes under test in isolation. Reach for a `*Scenario` helper (in [specs.scenarios.csharp.md](./specs.scenarios.csharp.instructions.md)) **only** when testing the very engine your repo provides — Arc → `CommandScenario`, Chronicle → the event/projection/reactor scenarios. The **cratis-application-slice-specifications** skill is application-oriented, not for general framework specs.
> - **Event-sourced applications on Cratis** (commands, projections, reducers, reactors, constraints): use this base plus a direct call by default for pure `Handle()`, validator and reducer decisions with supplied read-model state; add the scenarios its per-artifact decision table obligates — see **[specs.scenarios.csharp.md](./specs.scenarios.csharp.instructions.md)** (`profile: application`) and the **cratis-application-slice-specifications** skill.

`Cratis.Specifications` keeps the approach of Machine.Specifications (MSpec): `Establish → Because → should_` maps to "Given → When → Then" and keeps each spec focused on *one setup, one action, one set of assertions*.

## Frameworks

- [xUnit v3](https://xunit.net/docs/getting-started/v3/migration) is the house default for new and migrated spec projects: reference `Cratis.Specifications.XUnit.v3` **5.0** with `xunit.v3.mtp-off`, `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`, and set `<OutputType>Exe</OutputType>` in the project. The package supplies the `Specification` base, convention lifecycle and `ShouldXxx` assertions; the namespace remains `Cratis.Specifications`. Its `Cratis.Specifications` dependency supplies `Catch` and `SpecificationMethods`.
- 5.0 targets `net10.0` only. A spec project that must stay on net8.0/net9.0 stays on 4.x, and net48 on 3.x. A project that stays on xUnit v2 uses `Cratis.Specifications.XUnit`, with the same base and conventions.
- `.NUnit`/`.NUnit5`, `.TUnit` and the experimental `.Runner` are alternatives for repositories already built on them.
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
| `void Destroy()` | teardown once per eligible class by default | after each fact when per-fact lifecycle is selected |

The shared lifecycle is the default on xUnit v2 and v3 since 5.0; the package registers the framework, so delete any hand-written `[assembly: Xunit.TestFramework(...)]` (it fails with `CS0579`). Eligible classes derive from `Specification`, have one public constructor with no arguments or only framework-managed fixture arguments, and contain only plain facts. Construction, Establish/Because and Destroy run once; all facts share the instance and must not mutate it. `IDisposable` specifications also share the lifecycle since Cratis.Specifications 4.4.0: one construction and one `Dispose` after the last fact. Initialization failure fails every fact. Theories and `ITestOutputHelper` constructors keep per-fact execution. Keep shared lifecycle on; `[PerFactSpecificationLifecycle]` opts out only the class whose facts genuinely mutate shared state and cannot move the mutation into `Because`. `CRSPEC0008` (a fact modifies the shared context) is an error by default: move the change into `Establish` or `Because`. See the **cratis-specifications-csharp** skill's shared-lifecycle reference.

Run xUnit v3, TUnit and native-runner specifications as each project's built executable: restore and build once, then run its matching configuration/TFM output. Only identified VSTest projects use `dotnet test <project> --no-build`. Never default to solution-wide `dotnet test` in a mixed suite. Filters and the 4.x → 5.0 checklist are in the **cratis-specifications-csharp** skill's [running-specs reference](../../../skills/cratis-specifications-csharp/references/running-specs.md).

CRSPEC0001–CRSPEC0007 are warnings. Since 4.5 the analyzers ship only in `Cratis.Specifications`, transitively used by the framework packages; excluding analyzers on a framework package does not remove them. Derived xUnit/NUnit test attributes are recognized since 4.1.2. `Given`/`When` are aliases since 4.4 (declaring both aliases for one phase is CRSPEC0009), but house style stays `Establish`/`Because`. With warnings as errors, a non-`should_` fact, multiple specs per file (given contexts exempt), a fact on a given context, a `should_` without a test attribute, explicit base lifecycle calls, non-public specs with facts, or `Because` on a given context fail the build. Follow **cratis-specification-by-example**; do not suppress these diagnostics.

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

A change to a widely substituted interface runs the whole affected spec project,
not one spec. Grep for `Substitute.For<IThatInterface>` to find every substitute
that needs updating.

Catch exceptions for a unit under test with `Catch.Exception`:

```csharp
async Task Because() => _error = await Catch.Exception(_sut.DoSomething);
[Fact] void should_not_fail() => _error.ShouldBeNull();
```

## Assertion extension methods

Since 5.0 assertions live in `Cratis.Specifications`, with types forwarded by each framework package (namespace `Cratis.Specifications`); recompile and keep core/framework packages on the same version: `.ShouldEqual(expected)`, `.ShouldBeTrue()`, `.ShouldBeFalse()`, `.ShouldBeNull()`, `.ShouldNotBeNull()`, `.ShouldBeEmpty()`, `.ShouldNotBeEmpty()`, `.ShouldContain(item)`, `.ShouldNotContain(item)`, `.ShouldContainOnly(items)`, `.ShouldBeOfExactType<T>()`, `.ShouldBeGreaterThan(n)`, `.ShouldBeLessThan(n)`.

Additional helpers include `ShouldNotEqual`, `ShouldBeSame`/`ShouldNotBeSame`, `ShouldMatch(Func<T, bool>)`, `ShouldBeInRange`, inclusive comparisons `ShouldBeGreaterThanOrEqual`/`ShouldBeLessThanOrEqual`, `ShouldContainSingleItem`, `ShouldEachConformTo`, ordered collection `ShouldEqual(IEnumerable<T>)`, collection `ShouldContain(Predicate<T>)`, `ShouldBeAssignableFrom<T>`, and string `ShouldContain(substring, StringComparison)`. `Catch.Exception` accepts `Action` or `Func<Task>`; `Catch.Exception<T>(Action)` catches only T.

## Conventions

- Common usings come from `GlobalUsings.Specs.cs` (`Xunit`, `NSubstitute`, `Cratis.Specifications`, …) — don't duplicate them, and don't add a using for the system-under-test namespace.
- **`using` ordering (SA1210/SA1211):** non-aliased namespaces first, then a blank line, then `using <alias> = …` aliases sorted by alias name. Alias a type whose short name collides with a namespace segment (CS0118) — `using RegisterAuthorCmd = …` style, with a domain-meaningful alias, never a technical `Command`/`Event`/`Component` suffix.
- Single-statement assertion lambdas use expression-body form (RCS1021) — `[Fact] void should_x() => result.ShouldEqual(...)`, not a `{ … }` block. Don't break long `should_` lines; don't add blank lines between `should_` methods.
- **Prefer a concept's sentinel over a raw empty primitive.** When a `ConceptAs<T>` / `EventSourceId<T>` value is expected — as an argument, a mock setup, or an assertion — use its `NotSet` (or `Empty` / `New()`) static rather than a `string.Empty` / `Guid.Empty` / `0` that implicitly converts. It states intent and survives a sentinel-value change (see [concepts.md](./concepts.instructions.md)). Reserve raw `string.Empty` for genuinely non-concept `string` values (e.g. asserting a decrypted-to-empty JSON payload).

## Never sleep — await a fact, not a duration

**A spec never waits on the clock for the system under test.** `Thread.Sleep`, a bare `Task.Delay`, or a `SpinWait` before an assertion makes the spec pass because the machine happened to be fast enough, and it silently writes today's latency into the suite: the next fix that shifts timing in the observer/projection core then breaks specs that have nothing to do with it. That coupling — not the bugs being fixed — is the single largest source of regressions in Chronicle, and it is why fixes there keep getting rolled back.

Wait on a signal instead:

| Waiting for | Await |
|---|---|
| observers to catch up with an append | `appendResult.WaitForCompletion()` |
| client artifacts to be registered with the kernel | `eventStore.WaitForRegistration()` |
| an observer's state or position | `WaitTillActive` / `WaitTillSubscribed` / `WaitTillReachesEventSequenceNumber` / `WaitForState` (reactors, reducers, projections) |
| a job | the `JobsWaitHelpers` extensions |
| anything with no helper yet | build the signal: a `SemaphoreSlim` (or `TaskCompletionSource`) released by the code that observes the event, awaited under a timeout — `Source/Clients/XUnit.Integration/EventAppendCollection.cs` is the shipped example, and it never polls |

A **deadline** is not a sleep — every helper above takes a `timeout`, and a timeout is what turns a hang into a named failure. Sleeping *between* re-checks is: a poll loop is a completion signal you have not built yet, so add the signal rather than another `while` + `Task.Delay`.

Three delays are not waits at all and stay allowed — say which one it is in a comment: a test double that is **slow on purpose** so the test can observe it mid-flight (`Task.Delay(HandleTime)`), an **infrastructure readiness backoff** between retries of a connect/start that can legitimately fail, and a `Task.Delay(1)` / `Task.Yield()` that **widens an interleaving window** in a concurrency spec.

The sleeps that predate this rule are frozen per file in `.github/timing-coupling-baseline.txt`; `.github/workflows/timing-coupling-ratchet.yml` fails a pull request that raises any entry or adds a file. The numbers may only go down.

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

- [specs.md](./specs.instructions.md) — folder structure, naming, BDD philosophy (the base this extends).
- [specs.scenarios.csharp.md](./specs.scenarios.csharp.instructions.md) — **application profile**: the in-process scenario family (`CommandScenario` / `EventScenario` / `ReadModelScenario` / `ReactorScenario`) + out-of-process Chronicle integration, for event-sourced apps.
- [efcore.specs.md](./efcore.specs.instructions.md) — `DbContext` specs with SQLite in-memory.
- [frontend-testing.md](./frontend-testing.instructions.md) — TypeScript/React application BDD specs.
