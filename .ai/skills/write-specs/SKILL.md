---
name: write-specs
description: Use this skill to write specs for an event-sourced Cratis APPLICATION slice (command, query, projection, reactor, constraint) using the in-process scenario family (CommandScenario / EventScenario / ReadModelScenario / ReactorScenario); out-of-process Chronicle integration is an advanced fallback. NOT for framework/library code — that uses the plain Specification base (see specs.csharp.md).
---

Write comprehensive in-process BDD specs for an **event-sourced Cratis application** slice. **Lead with the scenario family**; reserve out-of-process Chronicle host specs for the host/transport boundary. Full patterns and assertion catalogs: `.ai/rules/specs.scenarios.csharp.md` (application profile).

> **Application-oriented.** Most **framework / library** specs (Chronicle kernel, Arc pipeline, Fundamentals, generators) use the plain `Specification` base + NSubstitute — see [specs.csharp.md](../../rules/specs.csharp.md). A framework repo reaches for the scenario family only to test the engine it provides (Arc's command pipeline, Chronicle's event/projection/reactor engine) — see [specs.scenarios.csharp.md](../../rules/specs.scenarios.csharp.md).

## Spec placement

Specs live **in the same slice folder** as the `.cs` file, each file wrapped in `#if DEBUG … #endif` (spec code ships only in Debug):

```
<Feature>/<Slice>/
├── <Slice>.cs
└── when_<verb_phrase>/
    ├── and_<happy_scenario>.cs
    └── and_<failure_scenario>.cs
```

## What to cover for every State Change command

One spec class for **each** of:

1. **Happy path** — command succeeds, expected event appended (`CommandScenario`)
2. **Each validation failure** — one `and_` class per `CommandValidator` / `ConceptValidator` rule (`CommandScenario`)
3. **Each business rule violation** — one `and_` class per DCB condition in `Handle()` that inspects a read model (`CommandScenario`)
4. **Each constraint violation** — one `and_` class per `IConstraint` → use the **write-specs-events** skill (`EventScenario`)

## Default: `CommandScenario<TCommand>` (in-process)

Runs authorization, validators, `Provide()`, and `Handle()` in-process and exposes the appended events — no HTTP, no fixture.

```csharp
#if DEBUG
namespace MyApp.Projects.Registration.when_registering;

public class and_name_is_unique : Specification
{
    readonly CommandScenario<RegisterProject> _scenario = new();
    readonly ProjectId _id = ProjectId.New();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Execute(new RegisterProject(_id, "My Project"));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] async Task should_have_appended_the_registered_event() =>
        await _scenario.ShouldHaveAppendedEvent<RegisterProject, ProjectRegistered>(_id, e => e.Name == "My Project");
}
#endif
```

**Every validation-failure spec asserts both** (never assert on message strings — they are presentation text):

```csharp
[Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();
[Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
```

- `ShouldNotBeSuccessful()` alone can't tell a validation rejection from an unhandled exception — pair it with `ShouldHaveValidationErrors()`.
- Authorization failures use `ShouldNotBeAuthorized()` (an unauthorized result has no validation errors).
- `CommandResult` assertions: `ShouldBeSuccessful()`, `ShouldNotBeSuccessful()`, `ShouldBeValid()`, `ShouldHaveValidationErrors()`, `ShouldHaveValidationErrorFor(message)`, `ShouldBeAuthorized()`, `ShouldNotBeAuthorized()`, `ShouldHaveExceptions()`.
- Seed prior DCB read-model state through `_scenario.Services` (substitute `IReadModels`); there is no `Given`/`Events` on `CommandScenario`.

## Other slice types — defer to the focused skills

- **Constraints / raw append semantics** → **write-specs-events** (`EventScenario`, `ShouldHaveConstraintViolationFor(name)`).
- **Projections & reducers** → **write-specs-readmodels** (`ReadModelScenario<TReadModel>`).
- **Reactors** → `ReactorScenario<TReactor>` (assert on mocked services); see `specs.scenarios.csharp.md`.
- **React / TypeScript surface** (view models, helpers, components) → **write-specs-frontend**.

## Naming conventions

- Folder: `when_<verb_phrase>` — e.g. `when_registering`
- File: `and_<condition>.cs` — e.g. `and_name_is_unique.cs`, `and_name_already_exists.cs`
- Method: `should_<expected_result>`

## After writing

Run `dotnet test` (Debug). Fix all failures before completing.

---

For full worked examples (happy path, validation, constraint via `EventScenario`) and the **advanced** out-of-process host pattern, see [references/EXAMPLES.md](references/EXAMPLES.md).
