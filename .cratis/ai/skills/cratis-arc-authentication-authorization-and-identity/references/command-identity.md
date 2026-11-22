<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/command-identity.md -->
# Command identity and Chronicle attribution

## Contents

- Choose the contract
- Supply an application actor
- Select a caller-keyed read model
- Attribute events without copying personal data

## Choose the contract

| Need | Contract |
| --- | --- |
| Identify the caller at the server boundary | `ICurrentPrincipalAccessor.Current` in `Cratis.Arc.Authorization` |
| Supply an application-owned actor value to `Provide()` or `Handle()` | `IAsyncParameterDependencyResolver` in `Cratis.Arc.DependencyInjection` |
| Select a Chronicle read model using the caller | `[LookUpBy<CallerSubject>]`, `CallerClaim` or `IReadModelKeyProvider` in `Cratis.Arc.Commands` |
| Decide whether the caller may perform the action | Named authorization policies and `[Authorize]`; `Provide()` may short-circuit only when acquisition yields nothing usable |
| Attribute an append | Chronicle's `IIdentityProvider.GetCurrent()` supplies `CausedBy`; observers read `EventContext.CausedBy` |

An actor value and a lookup key are neither proof of permission nor a substitute
for authorization. Derive them from the server-selected authenticated principal,
not a command's `UserId`, a readable identity cookie, or untrusted headers.
Configure the trusted authentication boundary first; see
[forwarded identity](../SKILL.md#read-this-first-what-is-and-is-not-trusted).
Anonymous callers get no privileged fallback. Never manufacture a System actor
to make an unresolved caller pass.

## Supply an application actor

Prefer a small, immutable application type over passing `ClaimsPrincipal` into
business decisions. The following is a complete resolver illustration, not a
host: it requires Arc's principal accessor and the service registration below.
`CurrentActor` and `ActorSubject` are application-owned types, not Arc APIs. This
application requires one distinct, nonempty exact `sub` claim and refuses the
System identity. Adapt that policy explicitly for verified workload callers;
do not silently substitute a system subject. Keep the raw subject inside the
application boundary; use a non-personal domain key where the domain needs one.

```csharp
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cratis.Arc.Authorization;
using Cratis.Arc.DependencyInjection;
using Cratis.Concepts;
using Microsoft.Extensions.DependencyInjection;

public record ActorSubject(string Value) : ConceptAs<string>(Value);
public record CurrentActor(ActorSubject Subject);

public sealed class CurrentActorResolver : IAsyncParameterDependencyResolver
{
    public bool TryResolve(IServiceProvider services, ParameterInfo parameter, out object? dependency)
    {
        dependency = null;
        if (parameter.ParameterType != typeof(CurrentActor)) return false;
        dependency = ActorFor(services.GetRequiredService<ICurrentPrincipalAccessor>());
        return true;
    }

    public ValueTask<ParameterDependencyResolution> Resolve(IServiceProvider services, ParameterInfo parameter) =>
        ValueTask.FromResult(TryResolve(services, parameter, out var dependency)
            ? ParameterDependencyResolution.Resolved(dependency)
            : ParameterDependencyResolution.NotResolved);

    static CurrentActor? ActorFor(ICurrentPrincipalAccessor principals)
    {
        if (principals.Current is not { Identity: { IsAuthenticated: true } } principal ||
            principal.Identity.AuthenticationType == "System") return null;

        var subjects = principal.Claims
            .Where(claim => claim.Type == "sub" && !string.IsNullOrEmpty(claim.Value))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .ToArray();
        return subjects is [var subject] && subject != "[System]"
            ? new CurrentActor(new ActorSubject(subject))
            : null;
    }
}
```

Register through the public resolver interface in the host's service setup:

```csharp
services.AddScoped<ICanResolveParameterDependency, CurrentActorResolver>();
```

`IAsyncParameterDependencyResolver` extends `ICanResolveParameterDependency`, so
both members matter. Awaiting command parameter binding calls `Resolve`; a
synchronous consumer calls `TryResolve`. Return
`ParameterDependencyResolution.NotResolved` (or `false`) for types you do not
own. `ParameterDependencyResolution.Resolved(null)` means the resolver owns the
parameter but has no value; it does not mean “try the next resolver”. The example
has no I/O. If acquisition needs I/O, await it in `Resolve`; do not block on a
Task in `TryResolve` or hide acquisition in `Handle()`.

Chronicle's registered chain consults Chronicle first, then application resolvers
from the current service scope. Do not replace its
`IInstancesOf<ICanResolveParameterDependency>` composition to install your actor
resolver, or claim a Chronicle read-model type as your application actor.

Require a named policy such as `[Authorize(Policy = "CanEnableFeature")]` on the
command and register that policy in the host. The policy owns the `CanEnable`
permission check, before validation or acquisition. A validator under read
protection `On` is an alternative for state-dependent preconditions, not caller
admission; see [validators are not an authorization boundary](../SKILL.md#validators-are-not-an-authorization-boundary).

These are **method excerpts** inside that command, not a complete slice.
`FeatureEnabled` is an application event with no identity fields:

```csharp
public AuthorizationResult Provide(CurrentActor? actor) =>
    actor is null
        ? AuthorizationResult.Failure("Actor unavailable.")
        : AuthorizationResult.Success;

public FeatureEnabled Handle(CurrentActor actor) => new();
```

The failed authorization result stops handler execution only because acquisition
yielded no usable actor, not because `Provide()` owns permission checks. A
non-nullable actor parameter alone is not a permission check. Do not use
`Provide()` to echo a client-supplied actor: ordinary returned values can satisfy
assignable `Handle()` parameters before resolver fallback. Keep acquisition in
`Provide()` or parameter binding and pass decision inputs explicitly; keep the
handler's decision free of hidden principal/service I/O. See
[authorization](authorization.md) and
[command decisions](../../cratis-arc-command/references/decisions-and-routing.md).

## Select a caller-keyed read model

Do not build a general actor resolver merely to select an existing Chronicle
read model. Arc already supports a generic lookup provider on a read-model
`Handle()` parameter. In a method excerpt where `CallerPreferences` is a
registered read model keyed by the caller's exact subject:

```csharp
public FeatureEnabled Handle([LookUpBy<CallerSubject>] CallerPreferences preferences) => new();
```

`CallerSubject` reads exact `sub`, without claim-type mapping. For another exact
claim, define a public parameterless provider such as:

```csharp
public sealed class CallerAccount() : CallerClaim("account_id");
```

Use `[LookUpBy<CallerAccount>]` on the read-model parameter. `CallerClaim` refuses
unauthenticated and System callers. `CommandCaller.Single` accepts one distinct
nonempty value using ordinal comparisons: repeated equal claims are one value;
different values are ambiguous.

For a derived key, implement `IReadModelKeyProvider.Resolve(ReadModelKeyContext)`.
Its `Caller` is the immutable selected `CommandCaller`, captured before command
context providers run; actor-chain claims are excluded. Its `ReadModelType` is
the target model. Return `ReadModelKeyLookup.Found(key)`, `.Missing` or
`.Ambiguous`. Providers require a public parameterless constructor and must be
stateless, thread-safe, synchronous, I/O-free and service-free. Do not consult
ambient identity, inject services, or decode an unvalidated token in a provider.

Missing/ambiguous results, provider failures, and a missing or changed captured
caller fail closed through `ReadModelKeyResolutionDenied`, mapped to an
unauthorized command result without caller/provider exception data. This is key
resolution, not proof that the selected model exists or that the action is
permitted. Do not combine generic and property lookup forms, replace a
caller-keyed model with a `Provide()` value, or use a caller provider to redirect
a per-stream model away from the command's own stream. See
[read-model injection](../../cratis-arc-command/references/read-model-injection.md)
for command-property lookup, absence and read protection.

## Attribute events without copying personal data

Chronicle append preparation calls `IIdentityProvider.GetCurrent()` and sends the
identity as `CausedBy`. `EventContext.CausedBy` exposes that attribution to
observers. `Identity.OnBehalfOf` links an identity chain. This is audit metadata,
not authentication, permission, or an event's compliance subject. Do not put
subjects, names, emails, usernames, roles, claims or actor objects in event payloads
merely to record who acted. Keep events about business facts.

Chronicle's ASP.NET provider maps exact `sub`, `name` and `preferred_username`,
with `Identity.Name` fallbacks for the latter two. Arc's identity decorator
preserves the selected Chronicle identity and also handles authenticated
server-side principals outside HTTP. When a principal has a `ClaimsIdentity.Actor`,
Arc records the actor first and the represented user as `Identity.OnBehalfOf`;
this chain is attribution, never an authorization grant.

For standalone Chronicle, configure `IIdentityProvider` at the application
boundary; `SetCurrentIdentity`/`ClearCurrentIdentity` control attribution, not
caller permissions. Never forward raw identifiers to external services, logs,
telemetry, or evaluators. For lawful personal event data and the separate erasure
subject, use [Chronicle compliance](../../cratis-chronicle-compliance/SKILL.md).
