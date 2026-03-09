---
applyTo: "**/*.cs"
---

# C# Conventions

The goal is minimal ceremony, maximum clarity. Modern C# (13+) gives us records, primary constructors, pattern matching, and file-scoped namespaces — use them everywhere. The less boilerplate in a file, the faster a reader can understand what it *does*.

## Building

- Use `dotnet build` from the command line.
- Use `dotnet format` to format code.
- Use `dotnet test` to run tests.

## Formatting

These rules exist so that every file in the codebase reads the same way. When formatting is consistent, code review focuses on logic, not style.

- Apply code-formatting style defined in `.editorconfig`.
- Use file-scoped namespace declarations — one less level of indentation for the entire file.
- Use single-line `using` directives, sorted alphabetically.
- Insert a blank line before the opening `{` of every code block (`if`, `for`, `foreach`, `try`, `using`, etc.).
- Ensure the final `return` statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible — they are more readable and the compiler verifies exhaustiveness.
- Use `nameof` instead of string literals — it survives refactoring.
- Place private class declarations at the bottom of the file — public API first, implementation details last.

## Naming

- PascalCase for type names, method names, and public members.
- camelCase for private fields and local variables.
- Prefix private fields with `_` (e.g. `_myField`).
- Prefix interface names with `I` (e.g. `IMyService`).

## Code Style

Every rule here reduces noise. `var` avoids redundant type repetition. Expression bodies eliminate braces for trivial members. Primary constructors remove the constructor-plus-field ceremony.

- Prefer `var` over explicit types — the right side of the assignment already tells you the type.
- Use expression-bodied members for simple methods and properties.
- Favor primary constructors for all types — they eliminate field declarations for injected dependencies.
- Use string interpolation instead of `string.Format()` or concatenation.
- Favor collection initializers and object initializers.
- Use `IEnumerable<T>` for collections that are not modified; never return mutable collections from public APIs.
- Don't use regions — they hide code instead of organizing it. If a file needs regions, it needs refactoring.
- Never add postfixes like `Async`, `Impl`, `Service` to class names — they add noise without information.
- For types with no implementation body, omit the braces (e.g. `public interface IMyInterface;`).
- Prefer `record` types for immutable data structures (events, commands, read models, concepts) — they give you value equality, immutability, and concise syntax for free.

## Nullable Reference Types

Embrace the type system — it is the first line of defense against null-related bugs. When it says something cannot be null, trust it.

- Use `is null` / `is not null` — never `== null` / `!= null`.
- Trust the C# null annotations; don't add defensive null checks when the type system guarantees a value.
- Add `!` operator where nullability warnings occur and you are certain the value is non-null.
- Use `is not null` checks before dereferencing potentially null values.

## XML Documentation

XML doc comments are the public API's first impression. They must be multiline — never cram `<summary>` onto a single line. Every public type, method, property, and operator must have XML docs.

- Always use **multiline** `<summary>` tags — opening and closing tags on their own lines:
  ```csharp
  /// <summary>
  /// Represents the unique identifier of a project.
  /// </summary>
  ```
- **Never** use single-line summaries:
  ```csharp
  // ❌ Wrong
  /// <summary>Represents the unique identifier of a project.</summary>
  ```
- Every method or operator with parameters **must** include `<param name="...">` for each parameter.
- Every method or operator that returns a value (non-void) **must** include `<returns>`.
- Every method that throws must document the exception with `<exception cref="...">` tags.
- Use `<see cref="..."/>` and `<paramref name="..."/>` to cross-reference types and parameters.
- Keep summaries concise and purposeful — only document when it adds understanding beyond the name itself.

Example:

```csharp
/// <summary>
/// Represents an instance of <see cref="ICommandFilters"/>.
/// </summary>
/// <param name="filters">The collection of <see cref="ICommandFilter"/> to use for filtering commands.</param>
[Singleton]
public class CommandFilters(IInstancesOf<ICommandFilter> filters) : ICommandFilters
{
    /// <summary>
    /// Filters the command execution through all registered command filters.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> to filter.</param>
    /// <returns>A <see cref="CommandResult"/> representing the aggregated filter outcome.</returns>
    public async Task<CommandResult> OnExecution(CommandContext context)
    {
        // ...
    }
}
```

## Exceptions

Every exception type in the codebase should communicate *what went wrong* in domain terms. Built-in types like `InvalidOperationException` tell you nothing about the problem — a custom `AuthorAlreadyRegistered` tells you everything.

- Use exceptions for exceptional situations only — never for control flow.
- Always create a custom exception type that derives from `Exception`.
- Never use built-in exception types (`InvalidOperationException`, `ArgumentException`, etc.).
- Never suffix exception class names with `Exception` — `AuthorNotFound` reads better than `AuthorNotFoundException`.
- Always provide a meaningful message when throwing.
- Add XML doc on the exception type starting with "The exception that is thrown when ...".

## Dependency Injection

The framework discovers and wires dependencies by convention. Explicit registration is the exception, not the rule.

- Prefer constructor injection; avoid `IServiceProvider` directly (service locator anti-pattern).
- For singletons, use the `[Singleton]` attribute — no explicit registration needed.
- Systems with a convention of `IFoo → Foo` do not need to be registered explicitly.
- Command/query `Handle()` method parameters are automatically resolved from DI by type.

## Logging

- Use structured logging with named parameters.
- Use `ILogger<T>` where `T` is the class name.
- Keep log messages in a separate `<ClassName>Logging.cs` partial static internal class.
- Use `[LoggerMessage]` attribute (without `eventId`).

## Async

- Use `async`/`await` for asynchronous programming.
- Use `Task` and `Task<T>` for asynchronous methods.

## File Header

Every C# file must start with:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```

## Generated Files

**Never edit generated files.** Files produced by code generators, scaffolding tools, or any other automated tool must not be modified by hand — in any language. Generated files are overwritten on the next build, so hand-edits are silently lost and create false confidence that a fix is in place.

- If the generated output is wrong, fix the **source** (the template, the generator configuration, or the source type) and rebuild.

## Chronicle & Arc — Key API Types

These are the building blocks. Each type has a specific role in the vertical slice architecture — using the right type in the right place means the framework handles discovery, wiring, and proxy generation automatically.

| Type | Purpose |
|---|---|
| `ConceptAs<T>` | Strongly-typed domain value wrapper (see concepts instructions) |
| `[EventType]` | Marks a record as a Chronicle event — **never** pass arguments to the attribute |
| `[Command]` | Marks a record as a model-bound command — define `Handle()` directly on the record |
| `[ReadModel]` | Marks a record as a model-bound query — define static query methods on the record |
| `CommandValidator<T>` | FluentValidation validator for commands |
| `IProjectionFor<T>` | Fluent projection definition — AutoMap is on by default |
| `IReducerFor<T>` | Imperative reducer — receives current state, returns new state |
| `IReactor` | Marker interface for side-effect observers — method dispatch by event type parameter |
| `IConstraint` | Constraint definition — enforced server-side by Chronicle at append time |
| `AggregateRoot` | Chronicle aggregate root with `Apply()` and `Commit()` |
| `ICommandPipeline` | Programmatic command execution from reactors or other code |
| `EventSourceId` | Primary identity binding — all events for an entity share the same event source ID |
| `EventContext` | Event metadata: `Occurred`, `SequenceNumber`, `CorrelationId`, `EventSourceId`, etc. |
| `ISubject<T>` | Observable query return type — enables real-time WebSocket push |
| `IMongoCollection<T>` | MongoDB collection — use `.Observe()` for reactive queries |
