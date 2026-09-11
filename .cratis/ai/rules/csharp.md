---
applyTo: "**/*.cs"
paths:
  - "**/*.cs"
---
<!-- cratis-ai-managed: rules/csharp.md -->


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

### Service lifetimes — `[Singleton]` is a narrow choice, not the default

**Assume every application you build is multi-tenant.** Not "design for it later" — assume it now, even when the deployment ships with a single tenant and no tenant resolution configured. A single-tenant application is a multi-tenant one with one tenant in it, and the code shape that serves both is the same shape. The code shape that serves only one has to be found and rewritten later, from the far side of a data migration, under production. The same reasoning applies to the signed-in user: an application always has one, and a service that remembers *which* one will eventually answer for the wrong person.

That gives one rule with two faces:

> **A singleton may not depend on anything that belongs to a tenant, a user, or a request.**

These resolve **per scope**, and the scope is what carries the tenant — so none of them may be injected into a `[Singleton]`:

| Off limits in a singleton | Why |
| --- | --- |
| `IEventStore` — and everything off it: `IEventLog`, `IReadModels`, `IConstraints`, `IEventTypes`, `IProjections`, `IReducers`, `IPII` | resolved for the scope's namespace |
| `IMongoCollection<T>`, `IMongoDatabase`, `IMongoClient` | the database name is resolved per scope from the current tenant |
| An EF Core `DbContext` | scoped for the same reason, plus it is not thread-safe |
| A read model injected directly by key | same scope, same binding |
| Any held tenant, principal, claims, correlation id, or `HttpContext` **value** | belongs to one request and outlives it in a singleton |

A `[Singleton]` taking one of these is a **captive dependency**: the container hands it the *root* scope's instance and keeps it for process lifetime. The root scope has no request, so it resolves no tenant — every read and write goes to the default namespace forever, regardless of who is asking.

**It does not throw. It returns nothing.** A query against the wrong namespace hits a database that exists and is empty, so the caller gets an empty collection, a `null` read model, or a default-valued options object, and carries on. The application starts, the pages render, the build is green, and the configuration a tenant spent an afternoon entering is simply not there. It is also invisible while there is only one tenant — every symptom appears on the day a second one arrives.

**What to use instead.** Default to the convention (transient), which inherits the resolving scope's tenant for free, or `[Scoped]` when a service must be shared within one request. Reserve `[Singleton]` for things that are genuinely process-wide and hold no tenant-, user-, or request-bound state: `IInstancesOf<T>` aggregators, HTTP client wrappers, `IOptions<T>` readers, pure computation, framework plumbing.

When something must be a singleton and still needs data — a hosted service, a dispatcher, a poller — inject `IServiceScopeFactory` and open a scope per unit of work:

```csharp
// ❌ Wrong — IEventStore is scoped; this captures the root scope's default namespace forever.
[Singleton]
public class DigestSources(IEventStore eventStore) : IDigestSources
{
    public Task<DigestConfiguration?> GetCurrent() =>
        eventStore.ReadModels.GetInstanceById<DigestConfiguration>(DigestId.Default);
}

// ✅ Right — a scope per call, so the collaborators bind to the caller's tenant.
[Singleton]
public class DigestSources(IServiceScopeFactory scopeFactory) : IDigestSources
{
    public async Task<DigestConfiguration?> GetCurrent()
    {
        using var scope = scopeFactory.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        return await eventStore.ReadModels.GetInstanceById<DigestConfiguration>(DigestId.Default);
    }
}
```

`IChronicleClient` **is** singleton-safe, and is the right collaborator when a flow knows which namespace it means and has no scope to resolve one from — it names the event store and namespace explicitly: `await chronicleClient.GetEventStore("MyStore", tenantId.Value)`. Naming the namespace is a deliberate, readable statement that this code crosses a tenant boundary; capturing a scoped service is the same crossing made by accident.

**The current user is not process-wide either.** Never keep the signed-in user, their principal, claims, roles, or anything derived from them in a singleton. The distinction that matters: *the accessor is fine, the value is not.* `IHttpContextAccessor` is itself a singleton and safe to inject; reading a value out of it once and keeping it is not. A current-user service may be a singleton only when every method reads through the accessor on each call and stores nothing. Anything that derives something per user and wants to keep it holds a cache **keyed by the user**, never a single field.

**Off-request work carries its tenant.** Reactors, hosted services, background dispatch and scheduled jobs run with no HTTP request, so there is nothing for a tenant resolver to read. Chronicle observers are themselves instantiated per namespace, but the collaborators they call are not — a reactor that reaches a tenant-blind singleton has left its namespace behind without saying so. Such a flow states its tenant explicitly rather than inheriting whatever the root scope happens to be.

**Caching.** A process-wide cache of tenant data is the same bug wearing a performance justification. If a singleton caches, the tenant (and where relevant the user) is part of the key. The same holds for `static` fields: a `static` cache of anything tenant-scoped is shared by every tenant in the process.

**Enforce it, do not remember it.** This failure is silent, so review will not reliably catch it. Add an architecture spec that reflects over the assembly, finds every `[Singleton]` whose constructor takes a scope-bound service, and asserts the set is empty. It is a few dozen lines, it runs on every build, and it is the only thing that keeps the rule true a year from now.

> .NET's own captive-dependency detection (`ServiceProviderOptions.ValidateScopes`, which Arc deliberately leaves on in Development) exists to catch exactly this. If a singleton in your codebase holds a scoped service and Development startup is not complaining, that path is not being exercised in Development — worth knowing on its own.

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
| --- | --- |
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
