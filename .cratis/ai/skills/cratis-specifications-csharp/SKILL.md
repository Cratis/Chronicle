---
name: cratis-specifications-csharp
description: Write C# specifications with Cratis.Specifications using the Establish/Because/should_ pattern and the for_/when_/and_ folder hierarchy. Use when adding or restructuring C# specs in any Cratis repository, choosing between an isolated unit spec and an in-process scenario spec, or building reusable given/ contexts. Do not use for TypeScript or React specs, and do not use it to decide what a command, projection, or reactor should do.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-specifications-csharp/SKILL.md -->

# Cratis C# specifications

Specifications are executable documentation. The folder tree reads like a table
of contents, and each file states one setup, one action, and one or more
focused assertions.

## Verified product sources

This skill is verified against these exact public releases:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Specifications` | `4.1.0` | `Specification` base, `Catch.Exception`, `ShouldXxx` assertions |
| `Cratis.Arc.Testing` | `22.10.4` | `CommandScenario<TCommand>` and `CommandResult` assertions |
| `Cratis.Chronicle.Testing` | `17.0.0` | `EventScenario`, `ReadModelScenario<T>`, `ReactorScenario<T>` |

Reverify against the owning product repository before claiming support for
another version. Never translate an assertion or helper name from memory.

## Route near misses

- The specification is TypeScript, React, or a view model: use
  `cratis-specifications-typescript` instead.
- The question is what a command, projection, reducer, or reactor *should do*:
  resolve the behavior first; this skill only specifies already decided
  behavior.
- The repository builds a Cratis library rather than an application: stay on the
  plain `Specification` base in this file and do not reach for the scenario
  family except to test the very engine that repository provides.

## Step 1 — Choose the specification surface

| Situation | Surface |
| --- | --- |
| Isolated class, collaborators can be substituted | `Specification` + NSubstitute |
| Arc command pipeline (validators, `Provide()`, `Handle()`, appended events) | `CommandScenario<TCommand>` |
| Chronicle append semantics, constraints, concurrency | `EventScenario` |
| Projection or reducer state from a sequence of events | `ReadModelScenario<TReadModel>` |
| Reactor invocation and its side effects | `ReactorScenario<TReactor>` |
| Host, transport, or real-infrastructure boundary | Out-of-process Chronicle integration specification |
| Setup shared by many specifications | Reusable context under `given/` |

The plain `Specification` base is the universal foundation and the dominant mode
in library and framework code. The four scenario helpers are the default for
event-sourced *application* behavior; read
[application-scenarios.md](references/application-scenarios.md) before using
one. Out-of-process integration specifications are an advanced case reserved for
boundaries the scenario helpers cannot reach — see
[integration-specs.md](references/integration-specs.md).

Specification projects are named `<Source>.Specs` and run on xUnit.

## Step 2 — Create the folder structure

```
for_<ClassName>/
├── given/
│   ├── all_dependencies.cs       ← substitutes every collaborator
│   └── a_<system_under_test>.cs  ← builds the SUT, inherits all_dependencies
├── when_<behavior>/              ← a behavior with multiple outcomes
│   ├── and_<condition>.cs
│   └── with_<state>.cs
└── when_<simple_behavior>.cs     ← a single outcome is a single file
```

Paths read as English sentences: `for_AuthorService / when_registering /
and_name_already_exists`. Allowed outcome prefixes are `and_`, `with_`,
`without_`, `having_`, and `given_`.

**`when` belongs only in a `when_<behavior>` folder name.** A specification file,
class, or non-`when_` folder must never contain the word `when`. Two "whens" in
one path is always wrong — fold the context into the `when_` folder name and use
preposition files for the outcomes.

```
# Wrong — two whens in the sentence path
when_appending_event_with_migrations/
└── with_a_registered_migration_when_appending_a_generation_1_event.cs

# Correct — context in the folder, outcomes are flat files
when_appending_event_with_registered_migration/
├── and_event_is_generation_1.cs
└── and_event_is_generation_2.cs
```

Add a sub-folder under `when_` only when that condition has its own multiple
outcomes.

## Step 3 — Write the specification

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace <RootNamespace>.for_<ClassName>;

public class when_<behavior> : Specification
{
    <CollaboratorType> _<collaborator>;
    <ResultType> _result;

    void Establish() => _<collaborator> = <setup>;

    void Because() => _result = <the single action under test>;

    [Fact] void should_<expected_outcome>() => _result.ShouldEqual(<expected>);
    [Fact] void should_<other_expected_outcome>() => _result.ShouldNotBeEmpty();
}
```

- `void Establish()` sets up the world. Each class in an inheritance chain has
  its own and they run base-first. Never call `base.Establish()`.
- `void Because()` performs **one** action. It belongs only in a concrete
  specification, never in a reusable context.
- `[Fact] void should_*()` carries one assertion. Use expression-body form and
  leave no blank line between `should_` methods.
- Fields are `private` in a concrete specification and `protected` in a `given/`
  context, named `_camelCase`.
- Any phase may be `async Task`. `void Destroy()` is the optional teardown.

`Cratis.Specifications` discovers `Establish`, `Because`, and `Destroy` by
convention — there are no attributes on them.

## Step 4 — Extract a reusable context

When several specifications share setup, layer it under `given/`:
`all_dependencies` substitutes the collaborators, the next context builds the
system under test, and each specification adds only what is unique.

```csharp
// for_<ClassName>/given/all_dependencies.cs
namespace <RootNamespace>.for_<ClassName>.given;

public class all_dependencies : Specification
{
    protected <CollaboratorType> _<collaborator>;

    void Establish() => _<collaborator> = Substitute.For<<CollaboratorType>>();
}
```

```csharp
// for_<ClassName>/given/a_<system_under_test>.cs
namespace <RootNamespace>.for_<ClassName>.given;

public class a_<system_under_test> : all_dependencies
{
    protected <ClassName> _<sut>;

    void Establish() => _<sut> = new(_<collaborator>);
}
```

Name a context `a_` or `an_` so it reads as "given an observer, when handling".
Full substitution, assertion, and exception-catching patterns are in
[csharp-patterns.md](references/csharp-patterns.md).

## Step 5 — Never wait on the clock

A specification never sleeps to let the system under test catch up.
`Thread.Sleep`, a bare `Task.Delay`, or a `SpinWait` before an assertion passes
because the machine happened to be fast enough and writes today's latency into
the suite. Await a signal instead:

| Waiting for | Await |
| --- | --- |
| Observers to catch up with an append | `appendResult.WaitForCompletion()` |
| Client artifacts registered with the kernel | `eventStore.WaitForRegistration()` |
| An observer's state or position | `WaitTillActive`, `WaitTillSubscribed`, `WaitTillReachesEventSequenceNumber`, `WaitForState` |
| Anything without a helper | A `SemaphoreSlim` or `TaskCompletionSource` released by the code that observes the event, awaited under a timeout |

A **deadline** is not a sleep: every helper takes a timeout, and a timeout turns
a hang into a named failure. Sleeping *between* re-checks is a sleep — a poll
loop is a completion signal that has not been built yet.

Three delays are not waits and stay allowed. Say which one it is in a comment: a
test double that is slow on purpose so the specification can observe it
mid-flight, an infrastructure readiness backoff between connect retries, and a
`Task.Delay(1)` or `Task.Yield()` that widens an interleaving window in a
concurrency specification.

## Step 6 — Apply the C# conventions

- Common usings come from `GlobalUsings.Specs.cs` (`Xunit`, `NSubstitute`,
  `Cratis.Specifications`). Do not duplicate them and do not add a using for the
  namespace of the system under test.
- Order usings with non-aliased namespaces first, a blank line, then
  `using <alias> = …` sorted by alias name. Alias a type whose short name
  collides with a namespace segment, using a domain-meaningful alias rather than
  a technical `Command`/`Event` suffix.
- Prefer a concept's own sentinel — `NotSet`, `Empty`, `New()` — over a raw
  `string.Empty`, `Guid.Empty`, or `0` that implicitly converts. It states intent
  and survives a sentinel change. Reserve raw primitives for genuinely
  non-concept values.
- Every file carries the repository license header.

## What not to specify

- Simple auto-properties and properties that return a constructor parameter.
- Simple delegation such as `public IEnumerable<Author> All => _list;`.
- Logging calls and trivial null checks.
- Anything a specification name starting with `when_getting_` or
  `when_returning_` would describe — that is a getter, not a behavior.

Specify decisions, transformations, branching business rules, and coordination
between collaborators. That is where defects hide.

## Verify

- Every specification file states one `Establish`, one `Because`, and one or
  more `should_` facts.
- `Because()` appears only in concrete specifications, never in a `given/`
  context.
- No path contains `when` outside a `when_<behavior>` folder name.
- Outcome files use an allowed preposition prefix.
- Field access modifiers and `_camelCase` naming match the surface.
- No `Thread.Sleep`, bare `Task.Delay`, or poll loop stands in for a signal; any
  remaining delay carries a comment naming which allowed case it is.
- Assertions use the `ShouldXxx` extension methods and never assert on a
  presentation message string.
- Nothing trivial or compiler-verified is specified.
- The file carries the repository license header.
- The specification project builds and its specifications pass against the
  verified package versions.
