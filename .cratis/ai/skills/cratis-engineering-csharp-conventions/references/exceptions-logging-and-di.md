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

## Service lifetimes — anything taking a scoped dependency is scoped or transient

**The rule, before any of the reasoning:**

> **A type that takes a scoped dependency is itself scoped or transient. If you
> are reaching for `[Singleton]` on something that needs the event store, a
> database, or a read model, the answer is to not make it a singleton.**

`[Singleton]` is the exception, not the default. It is for what is genuinely
process-wide *and* holds nothing belonging to a tenant, a user, or a request.
Everything else takes the convention (transient) or a scoped lifetime, and
inherits the resolving scope — and therefore the right tenant — for free.

**Assume every application is multi-tenant**, even when it ships with a single
tenant and no tenant resolution configured. A single-tenant application is a
multi-tenant one with one tenant in it, and the code shape that serves both is
the same. The shape that serves only one has to be found and rewritten later,
from the far side of a data migration, in production.

So the rule has a second face:

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

**What to do instead — in this order.**

**1. Drop `[Singleton]`.** This is the answer almost every time. Delete the
attribute and let the type be transient by convention, or mark it scoped when
one request should share one instance. Nothing else changes: the constructor
keeps the collaborator it wanted, and now gets the caller's tenant instead of
the root scope's. Reserve `[Singleton]` for what is genuinely process-wide and
holds no tenant-, user-, or request-bound state: implementation aggregators,
HTTP client wrappers, options readers, pure computation, framework plumbing.

```csharp
// Wrong — the scoped collaborator captures the root scope's default namespace forever
[Singleton]
public class <ClassName>(<IScopedCollaboratorType> <collaborator>) : <IInterfaceName>
{
    public Task<<ResultType>?> <MethodName>() => <collaborator>.<Method>(<argument>);
}

// Right — no attribute at all. Transient by convention, so it resolves in the
// caller's scope and reads that caller's tenant.
public class <ClassName>(<IScopedCollaboratorType> <collaborator>) : <IInterfaceName>
{
    public Task<<ResultType>?> <MethodName>() => <collaborator>.<Method>(<argument>);
}
```

**2. Only when the lifetime is forced on you, open a scope per unit of work.** A
hosted or background service is resolved once by the host, so it *is* a
singleton whether or not you asked — and it runs with no request to inherit a
scope from. That, and only that, is what `IServiceScopeFactory` is for:

```csharp
// Right for a hosted service — a scope per unit of work
public class <ClassName>(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var <collaborator> = scope.ServiceProvider
            .GetRequiredService<<IScopedCollaboratorType>>();

        await <collaborator>.<Method>(<argument>);
    }
}
```

`IServiceScopeFactory` is **not** a way to keep `[Singleton]` on a service that
had no reason to be one. It is more code, it hides the lifetime question behind
a scope nobody asked for, and — because a scope with no request still resolves
no tenant — it does not by itself make an off-request flow tenant-correct.
Reaching for it first is how a codebase ends up with dozens of these. If the
type is not the host's own, the fix is the attribute, not the factory.

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

**Enforce it, do not remember it.** This failure is silent, so review will not
reliably catch it. Two gates, and an application wants both.

Turn .NET's own scope validation on in **every** environment, not just
Development. `ValidateScopes` rejects resolving a scoped service from the root
provider, and `ValidateOnBuild` walks every registration at startup so a captive
dependency fails the host immediately rather than at whichever request first
needs it. The host enables both in Development by default and neither outside
it — which is backwards for a failure whose whole character is that it stays
quiet:

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

Enable it on an existing codebase in this order, or it will simply refuse to
start: turn it on locally first, fix everything it names, and only then let it
reach the deployed environments. Turning it on before the sweep converts a
silent multi-tenant bug into a production outage.

And add an architecture specification, because validation only catches what a
run actually resolves. Reflect over the assembly, find every `[Singleton]` whose
constructor takes a scope-bound service, and assert the set is empty. It names
every offender in one pass rather than one per restart, and it covers the types
no startup path touches.

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
