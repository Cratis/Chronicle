---
name: cratis-engineering-csharp-conventions
description: Apply the Cratis C# house conventions when writing or reviewing C# in a Cratis repository - formatting, naming, records and primary constructors, nullable handling, XML documentation, custom exceptions, structured logging, dependency injection, and service lifetimes. Use for any "how should this be written" C# style question; defer product API decisions and specification authoring to their focused workflows.
license: LICENSE
---
<!-- cratis-ai-managed: skills/cratis-engineering-csharp-conventions/SKILL.md -->

# Cratis C# engineering conventions

These are the house conventions Cratis maintainers apply across every
repository. They are **conventions**, not framework contracts: nothing here is
enforced by an analyzer unless this skill says so. Follow them for consistency;
do not claim the framework requires them.

## Route near misses

- The question is what a Cratis product API *does*: resolve it against the
  owning product repository, not against this style guide.
- The subject is a specification file: the specification conventions own the
  `Establish`/`Because`/`should_` pattern and the `for_`/`when_` hierarchy.
- The subject is TypeScript or React: this skill covers C# only.
- The subject is repository structure or documentation: those are separate
  workflows.

## Quick reference

- Use current C# language features — records, primary constructors, pattern
  matching, collection expressions.
- `var` over an explicit type; the right-hand side already names the type.
- File-scoped namespace declarations.
- `using` directives alphabetically sorted, single-line, unused ones removed.
- No regions. A file that needs them needs refactoring instead.
- No technical postfixes on type names: no `Impl`, `Service`, `Manager`,
  `Handler`, `Base`, `Async`.
- No `Exception` suffix on exception types — `AuthorNotFound`, not
  `AuthorNotFoundException`.
- Never throw a built-in exception type. Always define a domain exception.
- `record` for events, commands, read models, concepts, and DTOs.
- `is null` and `is not null` — never `== null` or `!= null`.
- Blank line before the opening `{` of every block.
- A final `return` sits on its own line.
- Private fields are `_camelCase`; interfaces take the `I` prefix.
- American English everywhere — initialize, behavior, color, serialize.
- Every file starts with the repository license header.

## Formatting

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace <RootNamespace>.<Feature>;

using <Namespace>.<First>;
using <Namespace>.<Second>;

// Blank line before the opening brace of every block
if (<condition>)
{
    <statement>;
}

// Expression-bodied form for simple members
public string <PropertyName> => $"{<First>} {<Second>}";

// The final return stands alone
public <ReturnType> <MethodName>()
{
    var result = <expression>;

    return result;
}
```

## Naming

| Artifact | Convention | Example |
| --- | --- | --- |
| Type, method, public member | PascalCase | `RegisterAuthor`, `AuthorId` |
| Private field | `_camelCase` | `_eventLog` |
| Local variable | camelCase | `authorId` |
| Interface | `I` prefix | `IEventLog` |
| Exception type | No `Exception` suffix | `AuthorNotFound` |
| Feature folder | Pluralized domain noun | `Authors/` |
| Concept file | The concept name | `AuthorId.cs` |

Avoid abbreviations unless they are universally known (`Id`, `Xml`, `Json`,
`Url`). Never add a prefix or postfix that names a technical role —
`Controller`, `ViewModel`, `Handler`, `Manager`, `Factory`, `Base`. Name after
the domain, not the pattern.

## Where the detail lives

| Topic | Reference |
| --- | --- |
| Records, primary constructors, `var`, collections, nullable, async, pattern matching, XML documentation | [code-style.md](references/code-style.md) |
| Custom exceptions, structured logging, dependency injection, service lifetimes, implementation discovery | [exceptions-logging-and-di.md](references/exceptions-logging-and-di.md) |
| CUPID, cohesion over layers, ubiquitous language, immutability | [domain-philosophy.md](references/domain-philosophy.md) |

Read the reference that covers the decision at hand rather than all three.

## The two rules most often got wrong

**`[Singleton]` is a narrow choice, not the default.** A singleton may not
depend on anything that belongs to a tenant, a user, or a request. Capturing a
scoped collaborator does not throw — it silently binds to the root scope's
default namespace forever and returns empty results. See
[exceptions-logging-and-di.md](references/exceptions-logging-and-di.md).

**Use `IInstancesOf<T>`, never `IEnumerable<T>`, to enumerate implementations of
an abstraction.** `IEnumerable<T>` only works when every implementation is
hand-registered, which defeats convention-based discovery.

## Verify

- Every file carries the repository license header and a file-scoped namespace.
- `using` directives are sorted, single-line, and free of unused entries.
- No regions, no technical postfixes, no `Exception` suffix.
- Every thrown exception is a domain type deriving from `Exception` with a
  meaningful message and an XML `<exception>` or `<summary>` doc starting with
  "The exception that is thrown when".
- No `catch` block is empty or silently swallowing.
- Null checks use `is null` / `is not null`, and no defensive check contradicts a
  non-nullable annotation.
- Every public type, method, property, and operator carries multiline XML
  documentation with `<param>` and `<returns>` where applicable.
- No `[Singleton]` holds tenant-, user-, or request-bound state.
- No `services.Add*<TInterface, TImplementation>()` registers a type that exists
  to be discovered by convention.
- Text is American English.
- The solution builds with zero warnings and zero errors, and the affected
  specifications pass.
