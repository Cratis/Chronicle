---
applyTo: "**/*.cs"
paths:
  - "**/*.cs"
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
- Never qualify a type that is already unambiguously in scope via a `using` directive. When two `using` directives introduce conflicting type names, qualify only the conflicting occurrences using the shortest unambiguous path (e.g. `Concepts.Events.Foo` or `Contracts.Events.Foo`) — do not add `using` aliases for every conflicting type.
- Insert a blank line before the opening `{` of every code block (`if`, `for`, `foreach`, `try`, `using`, etc.).
- Ensure the final `return` statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible — they are more readable and the compiler verifies exhaustiveness.
- Use `nameof` instead of string literals — it survives refactoring.
- Place private class declarations at the bottom of the file — public API first, implementation details last.

## Language — American English Only

All identifiers, comments, XML docs, and string literals must use **American English** spelling (initialize, serialize, behavior, color, organization, center, modeling, dialog, license, judgment, gray). See [general.md](./general.md) for the full guidance.

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
- Prefer LINQ (`.Where`, `.Any`, `.Select`, `.FirstOrDefault`) over a `foreach` that filters inside its body with an `if`/`continue` or an early `return` — put the filter in the query so the intent is explicit (`items.Where(predicate)`, `return items.Any(predicate);`). Reserve `foreach` for genuine iteration with side effects. (Filtering inside a loop is what the analyzers flag as a "missed opportunity to use Where".)
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
- Never write an empty or silently-swallowing `catch` block. Handle the exception, log it, or let it propagate. When ignoring is genuinely correct, use an exception filter (`catch (SomeException) when (…)`) with a body that states the decision (a comment and/or a fallback) — never a bare `catch { }`.

## Dependency Injection

The framework discovers and wires dependencies by convention. Explicit registration is the exception, not the rule.

- Prefer constructor injection; avoid `IServiceProvider` directly (service locator anti-pattern).
- For singletons, use the `[Singleton]` attribute — no explicit registration needed.
- Systems with a convention of `IFoo → Foo` do not need to be registered explicitly.
- Command/query `Handle()` method parameters are automatically resolved from DI by type.

### Discovering multiple implementations — use `IInstancesOf<T>`, never `IEnumerable<T>`

When a type needs every implementation of an abstraction (handlers, strategies, filters, validators, formatters), inject `IInstancesOf<TInterface>` from `Cratis.Types`. The framework discovers and instantiates every implementation by convention — no `services.AddSingleton<TInterface, Impl1>()` calls anywhere.

```csharp
// ❌ Wrong — requires hand-maintained registrations for every implementation.
// Adding a new IReactorSideEffectHandler somewhere else in the codebase silently
// does nothing until someone remembers to register it here, and dead registrations
// linger after types are removed.
services.AddSingleton<IReactorSideEffectHandler, EventResultHandler>();
services.AddSingleton<IReactorSideEffectHandler, EventsResultHandler>();
services.AddSingleton<IReactorSideEffectHandlers, ReactorSideEffectHandlers>();

public class ReactorSideEffectHandlers(IEnumerable<IReactorSideEffectHandler> handlers) : IReactorSideEffectHandlers { ... }

// ✅ Right — implementations discovered automatically. Mark singletons with [Singleton].
[Singleton]
public class EventResultHandler(IEventTypes eventTypes) : IReactorSideEffectHandler { ... }

[Singleton]
public class EventsResultHandler(IEventTypes eventTypes) : IReactorSideEffectHandler { ... }

[Singleton]
public class ReactorSideEffectHandlers(IInstancesOf<IReactorSideEffectHandler> handlers) : IReactorSideEffectHandlers { ... }
```

**Rules:**
- Never inject `IEnumerable<TInterface>` to enumerate implementations of an abstraction. That signature only works if every implementation is hand-registered, which defeats convention-based discovery. Use `IInstancesOf<TInterface>` instead.
- Never write `services.AddSingleton<TInterface, Impl>()` or `services.AddTransient<TInterface, Impl>()` for a type that exists to be discovered. Mark the implementation with `[Singleton]` (for singletons) or rely on the `IFoo → Foo` convention (for transients) and remove the registration line.
- `IInstancesOf<T>` resolves at the point of access — implementations added later in the assembly become available without touching the consumer or any composition root.
- `IEnumerable<T>` is still the right type to **return** from a method that yields a sequence of values. The rule applies only when the goal is to enumerate **implementations** of an abstraction.

## Logging

- Use structured logging with named parameters.
- Use `ILogger<T>` where `T` is the class name.
- Keep log messages in a separate `<ClassName>Logging.cs` partial static internal class.
- Use `[LoggerMessage]` attribute (without `eventId`).

## Async

- Use `async`/`await` for asynchronous programming.
- Use `Task` and `Task<T>` for asynchronous methods.

## Chronicle & Arc — Key API Types

These are the building blocks. Each type has a specific role in the vertical slice architecture — using the right type in the right place means the framework handles discovery, wiring, and proxy generation automatically.

| Type | Purpose |
|---|---|
| `ConceptAs<T>` | Strongly-typed domain *value* wrapper (see [concepts.md](./concepts.md)) |
| `EventSourceId<T>` | Strongly-typed *identity* base — derive event-source ids from this, not `ConceptAs<T>` |
| `[EventType]` | Marks a record as a Chronicle event — **never** pass arguments for a new event |
| `[Command]` | Marks a record as a model-bound command — define `Handle()` directly on the record |
| `[ReadModel]` | Marks a record as a model-bound query — define static query methods on the record |
| `CommandValidator<T>` | FluentValidation validator for commands |
| `IProjectionFor<T>` | Fluent projection definition — AutoMap is on by default, never call `.AutoMap()` |
| `IReducerFor<T>` | Imperative reducer — receives current state, returns new state |
| `IReactor` | Marker interface for side-effect observers — method dispatch by event type parameter |
| `IConstraint` | Constraint definition — enforced server-side by Chronicle at append time |
| `AggregateRoot` | Chronicle aggregate root with `Apply()` and `Commit()` |
| `ICommandPipeline` | Programmatic command execution from reactors or other code |
| `EventContext` | Event metadata: `Occurred`, `SequenceNumber`, `CorrelationId`, `EventSourceId`, etc. |
| `ISubject<T>` | Observable query return type — enables real-time push |
| `IMongoCollection<T>` | MongoDB collection — use `.Observe()` for reactive queries |

**Key conventions:**
- Prefer `ConceptAs<T>` over raw primitives in all domain models, commands, events, and queries; derive identity concepts from `EventSourceId<T>`. See [concepts.md](./concepts.md) for details.
- Projections join **events**, never read models — projections rebuild state from the event stream, not from other projections.
- For fluent projections, AutoMap is on by default — call `.From<EventType>()` without `.AutoMap()` and without manually mapping every matching property.
- Use model-bound projection attributes (`[FromEvent<T>]`, `[SetFrom<T>]`, etc.) when possible; fall back to `IProjectionFor<T>` for complex cases.
- Full slice anatomy lives in [vertical-slices.md](./vertical-slices.md).
