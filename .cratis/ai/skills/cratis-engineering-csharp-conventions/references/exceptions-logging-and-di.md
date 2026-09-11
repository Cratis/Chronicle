<!-- cratis-ai-managed: skills/cratis-engineering-csharp-conventions/references/exceptions-logging-and-di.md -->
# Exceptions, logging, and dependency injection

## Exceptions

Every exception type communicates *what went wrong in domain terms*. A built-in
`InvalidOperationException` says nothing; a domain `AuthorAlreadyRegistered`
says everything.

- Throw only for genuinely exceptional situations, never for control flow.
- Always define a custom type deriving from `Exception`. Never throw a built-in
  exception type.
- Never suffix the type name with `Exception`.
- Always supply a meaningful message.
- Document the type with an XML summary starting "The exception that is thrown
  when …".
- Never write an empty or silently swallowing `catch`. Handle it, log it, or let
  it propagate. When ignoring is genuinely correct, use an exception filter
  (`catch (<ExceptionType>) when (<condition>)`) whose body states the decision
  through a comment or a fallback — never a bare `catch { }`.

```csharp
/// <summary>
/// The exception that is thrown when <condition>.
/// </summary>
/// <param name="<parameterName>">The <see cref="<ParameterType>"/> that <description>.</param>
public class <DomainExceptionName>(<ParameterType> <parameterName>)
    : Exception($"<message> '{<parameterName>}' <detail>");
```

```csharp
var <identifier> = await <source>.<FindMethod>(<argument>)
    ?? throw new <DomainExceptionName>(<argument>);
```

## Logging

- Use structured logging with named parameters.
- Inject `ILogger<T>` where `T` is the containing class.
- Keep message definitions in a separate `<ClassName>Logging.cs` file as a
  `static partial` internal class.
- Use the `[LoggerMessage]` attribute and do **not** supply an `eventId`.
- Choose the level deliberately: `Information`, `Warning`, `Error`, `Debug`.

```csharp
// <ClassName>Logging.cs
namespace <RootNamespace>.<Feature>;

static partial class <ClassName>Logging
{
    [LoggerMessage(LogLevel.Information, "<message> '{<Parameter>}'")]
    internal static partial void <MessageName>(
        this ILogger<<ClassName>> logger, <ParameterType> <parameter>);
}
```

```csharp
public class <ClassName>(ILogger<<ClassName>> logger)
{
    public Task <MethodName>(<ParameterType> <parameter>)
    {
        logger.<MessageName>(<parameter>);
        <statement>;
    }
}
```

## Dependency injection

The framework discovers and wires dependencies by convention. Explicit
registration is the exception, not the rule.

- Prefer constructor injection. Never inject `IServiceProvider` to resolve
  collaborators — that is the service-locator anti-pattern.
- Mark a singleton with the `[Singleton]` attribute rather than registering it
  explicitly.
- A convention-based `IFoo → Foo` pair needs no registration.
- Command and query `Handle()` parameters resolve from DI by type.

```csharp
// Preferred — constructor injection
public class <ClassName>(<ICollaboratorType> <collaborator>);

// Avoid — service locator
public class <ClassName>(IServiceProvider provider)
{
    void <MethodName>() =>
        provider.GetService<<ICollaboratorType>>()!.<Method>();
}
```

## Service lifetimes — `[Singleton]` is a narrow choice

**Assume every application is multi-tenant**, even when it ships with a single
tenant and no tenant resolution configured. A single-tenant application is a
multi-tenant one with one tenant in it, and the code shape that serves both is
the same. The shape that serves only one has to be found and rewritten later,
from the far side of a data migration, in production.

That gives one rule with two faces:

> **A singleton may not depend on anything that belongs to a tenant, a user, or
> a request.**

These resolve **per scope**, and the scope carries the tenant, so none may be
injected into a `[Singleton]`:

| Off limits in a singleton | Why |
| --- | --- |
| The scoped event store and everything reached from it — event log, read models, constraints, event types, projections, reducers, PII | Resolved for the scope's namespace |
| A MongoDB collection, database, or client | The database name resolves per scope from the current tenant |
| An EF Core `DbContext` | Scoped for the same reason, and not thread-safe |
| A read model injected directly by key | Same scope, same binding |
| Any held tenant, principal, claims, correlation id, or HTTP context **value** | Belongs to one request and would outlive it |

A `[Singleton]` taking one of these is a **captive dependency**: the container
hands it the *root* scope's instance and keeps it for process lifetime. The root
scope has no request, so it resolves no tenant — every read and write goes to
the default namespace forever, regardless of who is asking.

**It does not throw. It returns nothing.** A query against the wrong namespace
hits a database that exists and is empty, so the caller receives an empty
collection, a null read model, or a default-valued options object and carries
on. The application starts, pages render, the build is green, and configuration
a tenant spent an afternoon entering is simply absent. It is invisible while
there is one tenant; every symptom appears the day a second arrives.

**What to use instead.** Default to the convention (transient), which inherits
the resolving scope's tenant for free, or a scoped lifetime when a service must
be shared within one request. Reserve `[Singleton]` for what is genuinely
process-wide and holds no tenant-, user-, or request-bound state: implementation
aggregators, HTTP client wrappers, options readers, pure computation, framework
plumbing.

When something must be a singleton and still needs data — a hosted service, a
dispatcher, a poller — inject `IServiceScopeFactory` and open a scope per unit
of work:

```csharp
// Wrong — the scoped collaborator captures the root scope's default namespace forever
[Singleton]
public class <ClassName>(<IScopedCollaboratorType> <collaborator>) : <IInterfaceName>
{
    public Task<<ResultType>?> <MethodName>() => <collaborator>.<Method>(<argument>);
}

// Right — a scope per call, so collaborators bind to the caller's tenant
[Singleton]
public class <ClassName>(IServiceScopeFactory scopeFactory) : <IInterfaceName>
{
    public async Task<<ResultType>?> <MethodName>()
    {
        using var scope = scopeFactory.CreateScope();
        var <collaborator> = scope.ServiceProvider
            .GetRequiredService<<IScopedCollaboratorType>>();

        return await <collaborator>.<Method>(<argument>);
    }
}
```

A client that names its store and namespace explicitly **is** singleton-safe,
and is the right collaborator when a flow knows which namespace it means and has
no scope to resolve one from. Naming the namespace is a deliberate, readable
statement that this code crosses a tenant boundary; capturing a scoped service
is the same crossing made by accident.

**The current user is not process-wide either.** Never keep the signed-in user,
their principal, claims, roles, or anything derived from them in a singleton.
The distinction that matters: *the accessor is fine, the value is not.* An HTTP
context accessor is itself a singleton and safe to inject; reading a value out
of it once and keeping it is not. A current-user service may be a singleton only
when every method reads through the accessor on each call and stores nothing.
Anything that derives something per user and wants to keep it holds a cache
**keyed by the user**, never a single field.

**Off-request work carries its tenant.** Reactors, hosted services, background
dispatch, and scheduled jobs run with no request, so a tenant resolver has
nothing to read. Observers may be instantiated per namespace, but the
collaborators they call are not — a flow that reaches a tenant-blind singleton
has left its namespace behind without saying so. Such a flow states its tenant
explicitly rather than inheriting whatever the root scope happens to be.

## Discovering implementations — `IInstancesOf<T>`, never `IEnumerable<T>`

When a type needs every implementation of an abstraction — handlers, strategies,
filters, validators, formatters — inject `IInstancesOf<TInterface>` from
`Cratis.Types`. The framework discovers and instantiates every implementation by
convention, so no explicit registration exists anywhere.

```csharp
// Wrong — hand-maintained registrations. A new implementation added elsewhere
// silently does nothing until someone remembers this file, and dead
// registrations linger after types are removed.
services.AddSingleton<<IHandlerType>, <FirstHandler>>();
services.AddSingleton<<IHandlerType>, <SecondHandler>>();

public class <AggregatorName>(IEnumerable<<IHandlerType>> handlers) : <IAggregatorType>;

// Right — implementations discovered automatically
[Singleton]
public class <FirstHandler>(<CollaboratorType> <collaborator>) : <IHandlerType>;

[Singleton]
public class <SecondHandler>(<CollaboratorType> <collaborator>) : <IHandlerType>;

[Singleton]
public class <AggregatorName>(IInstancesOf<<IHandlerType>> handlers) : <IAggregatorType>;
```

Rules:

- Never inject `IEnumerable<TInterface>` to enumerate implementations of an
  abstraction. That signature works only when every implementation is
  hand-registered, which defeats convention-based discovery.
- Never register a type that exists to be discovered. Mark it `[Singleton]`, or
  rely on the `IFoo → Foo` convention for a transient, and delete the
  registration line.
- `IInstancesOf<T>` resolves at the point of access, so an implementation added
  later in the assembly becomes available without touching the consumer or any
  composition root.
- `IEnumerable<T>` remains the right type to **return** from a method that
  yields a sequence of values. The rule applies only to enumerating
  *implementations* of an abstraction.
