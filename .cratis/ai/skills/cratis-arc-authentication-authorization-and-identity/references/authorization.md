<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/authorization.md -->
# Authorization

Verified against `Cratis.Arc.Core` and `Cratis.Arc` `22.41.1`. Everything below
describes that version and is in `Cratis.Arc.Authorization` unless stated.

## Versions differ — check the one you run

Authorization changed a lot between the version this skill used to pin
(`22.16.0`) and `22.41.1`. Advice that is right for one range is wrong for
another.

| Version | Behavior |
| --- | --- |
| through `22.20.0` (includes `22.16.0`) | Only Arc's own attributes count, and only `Roles` is evaluated: `Policy` and `AuthenticationSchemes` are read by nothing, so `[Authorize(Policy = "…")]` enforces nothing. Microsoft's `[Authorize]`/`[AllowAnonymous]` are **not** enforced on model-bound commands and read models (Arc's ASP.NET evaluators bind to Arc's same-named attributes), so a command protected only with Microsoft's attribute is open to every caller. Only the first attribute or evaluator that answers is used. |
| `22.21.0` | Microsoft's `[Authorize]`/`[AllowAnonymous]` are enforced on the ASP.NET Core host. Every stacked requirement applies (AND). Anonymous plus restricted on one member is rejected as ambiguous. Roles are still the only requirement evaluated. |
| `22.23.0` | Named policies and `AuthenticationSchemes` are evaluated (`IAuthorizationPolicy`, `AddArcAuthorizationPolicy<T>`, ASP.NET Core policies). |
| `22.26.0` | `Cratis:Arc:Introspection` options. |
| `22.30.0` | `IFallbackAuthorizationEvaluator` and `AuthorizationPolicyContext.ReceivedAt`. |
| `22.31.0` | Opt-in policy evaluation for unauthenticated callers. |
| `22.39.0` | Protected decisions arrive; the validator boundary at the end of this page is verified at `22.41.1`. |

If you are on a version before `22.21.0`, either upgrade, or use only Arc's
attributes with roles and treat everything else as unenforced. Do not write a
policy-protected endpoint on a version before `22.23.0`. The rest of this page
is `22.41.1` behavior.

## Attributes

| Attribute | Notes |
| --- | --- |
| `[AllowAnonymous]` | Arc's: class or method, no arguments |
| `[Authorize]` | Arc's: class or method, repeatable; settable `Policy`, `Roles` (comma-delimited, OR), `AuthenticationSchemes` |
| `[Roles(params string[] roles)]` | Arc's: derives from Arc's `[Authorize]` and joins the roles with commas |

There is **no** `[Policy]`, `[Scopes]` or `[Claims]` attribute in Arc.

With `Cratis.Arc` (ASP.NET Core host), Arc's pipeline **also enforces Microsoft's**
`[Authorize]` and `[AllowAnonymous]` on model-bound commands and read models,
including their policies and schemes, over mapped HTTP and hub subscriptions
(`22.21.0` and later). The two families can be mixed. Arc's own attributes are
the portable choice: they are enforced on every Arc host, whereas on the
standalone Core host nothing reads Microsoft's. `RolesAttribute` derives from
**Arc's** `AuthorizeAttribute`, never Microsoft's.

Analyzers: `ARC0011` (use `nameof` in `[Roles]`), `ARC0019` (`[AllowAnonymous]`
conflicts with `[Authorize]`/`[Roles]` on one declaration), `ARC0020` (an ASP.NET
Core attribute is not enforced without Arc's ASP.NET Core integration), `ARC0021`
(`AuthenticationSchemes` needs the ASP.NET Core host). Not every older NuGet
version contains every rule.

## Composition and precedence

- **Stacked requirements AND.** Every attribute on a declaration applies. A role
  list is the only OR: the caller needs **any one** role of a single list.
  Policies and roles are never OR'd with each other.
- **A method replaces its type.** A method's declaration replaces its type's
  declaration; it does not combine with it, whichever attribute family each
  comes from. A method-level `[AllowAnonymous]`, or a guest-allowed policy on a
  method, removes a type-level `[Roles("Admin")]` protection from that method.
- **Anonymous plus restricted is ambiguous.** `[AllowAnonymous]` with `[Authorize]`
  or `[Roles]` on one member throws `AmbiguousAuthorizationLevel`, in either
  attribute family (Microsoft's is read with `inherit: true`).
- **No attribute at all** means no authentication or role requirement, unless a
  fallback evaluator supplies a baseline (below).

## Named policies

```csharp
public class ActiveSubscription : IAuthorizationPolicy
{
    public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(context.Principal.HasClaim("subscription", "active"));
}

builder.Services.AddArcAuthorizationPolicy<ActiveSubscription>("ActiveSubscription"); // before Build()
```

```csharp
[Authorize(Policy = "ActiveSubscription")]
[Command]
public record <Name>(...)
```

- `IAuthorizationPolicy.IsAuthorized(AuthorizationPolicyContext, CancellationToken)`
  returns `ValueTask<bool>`. The policy is registered **scoped** and resolves
  from the command's or query's executing scope, so it may depend on scoped
  services.
- `AuthorizationPolicyContext` is `(ClaimsPrincipal Principal, MemberInfo Target, object Resource)`
  plus `ReceivedAt`. `Target` is the command type or query method; `Resource` is
  the executing `CommandContext` or `QueryContext`.
- `ReceivedAt` is **Arc receipt time**, captured once at transport dispatch (or
  direct pipeline entry, or per hub subscribe operation) and fixed across
  delayed or repeated evaluation. Use it for time-based admission instead of
  reading the clock after an asynchronous lookup. It is not network arrival
  time. A default value means no receipt: deny.
- Register every policy before the host starts. An unknown or duplicate name
  fails startup, and an unresolved policy at runtime never grants access. Register
  a name natively **or** in ASP.NET Core, not both.
- On the ASP.NET Core host, Arc also evaluates policies registered with
  ASP.NET Core (`AddAuthorizationBuilder().AddPolicy(...)`), referenced by either
  attribute family. The handlers receive Arc's `CommandContext`/`QueryContext`
  as the resource, not an MVC resource.
- The old synchronous `IAuthorizationEvaluator.IsAuthorized(Type|MethodInfo)`
  **rejects policy-bearing declarations** (`AsynchronousAuthorizationRequired`).
  Use the command or query pipeline.
- The standalone Core host fails startup on `AuthenticationSchemes`. The ASP.NET
  Core host authenticates each named scheme, combines the identities of the ones
  that succeed, and denies when none does; a request that selects schemes runs
  in a fresh execution scope. `ICurrentPrincipalAccessor` then exposes the
  selected identity.

## Guests and anonymous policies

By default **a policy requires an authenticated principal**, even when the policy
body would allow an anonymous caller. A guest is evaluated only after an explicit
opt-in:

- Arc policy: `AddArcAuthorizationPolicy<T>(name, evaluatesAnonymous: true)`. The
  policy receives an empty unauthenticated `ClaimsPrincipal` and decides itself.
- ASP.NET Core policy: register it as usual **and** call
  `AddArcAnonymousAspNetAuthorizationPolicy(name)`. The policy must not call
  `RequireAuthenticatedUser()` and must not select authentication schemes. The
  opt-in name must equal the declaration's name exactly, **case-sensitive**.
  The standalone Core host cannot use this opt-in.
- Startup rejects a missing, ambiguous, native, authentication-required or
  scheme-selected opt-in.
- Only a declaration made **entirely** of opted-in policies, with no roles and no
  schemes, can evaluate guests. Roles and schemes always require authentication.

⚠️ The opt-in governs Arc's pipeline verdict only. Today the HTTP layer can reject
a guest before the policy runs: a Core host with any authentication handler, or an
ASP.NET Core `FallbackPolicy` that requires authentication, returns 401 first
(tracked in Cratis/Arc#2948). Test guest endpoints through the real HTTP host.

⚠️ Because a method replaces its type, a method-level guest policy on a read model
with a type-level `[Roles("Admin")]` is reachable by guests when the policy
allows them. The type-level role no longer protects that method.

## Evaluation order

1. Anonymous evaluators and attribute evaluators resolve the declaration: method
   first, then its type, then the fallback baseline. Conflicts throw
   `AmbiguousAuthorizationLevel`.
2. On the ASP.NET Core host, named schemes are authenticated and the selected
   identity chosen.
3. Command context-value providers and the execution-scope `Begin` hooks run
   next, **before the policy verdict**, so they can run for a caller the policy
   later denies. They must not perform irreversible effects.
4. Authentication and roles are checked, and every policy is **awaited**, one at a
   time, **before** `Provide()`, `Handle()` or the query method runs.
5. Identity and declaration continuity is rechecked before and after each policy
   and again immediately before the handler or query method; a change denies.
6. Only then does `Provide()`/`Handle()`/the query method run.

⚠️ Policy checks gate **admission**. They do not revoke: a running observable
stream or a deferred enumeration is not rechecked, and a change in an external
permission store after admission is not detected. See
[observable emission guards](observable-emission-guards.md).

## Baselines: two separate mechanisms

**`IFallbackAuthorizationEvaluator`** supplies baseline requirements for commands
and queries that have **no explicit declaration**, on both hosts. Implement it on
a public class; it is discovered by convention, do not register it.

```csharp
public class MembersByDefault : IFallbackAuthorizationEvaluator
{
    public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(Type type) =>
        [AuthorizationRequirement.FromRoles("Member")];

    public IEnumerable<AuthorizationRequirement> GetAuthorizationRequirements(MethodInfo method) => [];
}
```

- Commands and type-targeted queries consult only the `Type` overload; the
  `MethodInfo` overload applies only to query methods. Put a baseline for all
  commands in the `Type` overload.
- A query method's explicit declaration, or its type's, replaces the baseline.
  With neither, the method and type baselines **all** apply, across evaluators.
- `[AllowAnonymous]` still wins; a method or type `[Authorize]` replaces the
  baseline. It returns requirements, not verdicts: use a policy or a filter for
  resource-dependent rules.
- `IAuthorizationAttributeEvaluator` reports only **explicit** declarations from
  another attribute family; it never becomes a baseline.

**ASP.NET Core `FallbackPolicy`** covers HTTP endpoints and MVC actions without
authorization metadata. It does **not** supply Arc's pipeline baseline, and
`IFallbackAuthorizationEvaluator` does not cover direct HTTP GET and WebSocket
requests to **controller-based** queries or controller-based commands (those are
MVC actions). Configure both: the evaluator for the Arc pipeline, `FallbackPolicy`
for MVC. Neither protects an endpoint that is mapped explicitly anonymous (see the
discovery endpoints in the skill).

## Default access by hosting model

There is no `ArcOptions` switch for a default requirement. What you get depends
on the pipeline.

### Self-hosted `ArcApplication` (Arc.Core)

The authentication middleware runs per request:

- **No `IAuthenticationHandler` registered → the middleware lets the request
  through** (every endpoint is effectively anonymous), except an endpoint whose
  metadata sets `RequireAuthentication`, which throws
  `AuthenticationRequiredWithoutHandlers` instead.
- With at least one handler, an endpoint marked `AllowAnonymous` passes and every
  other endpoint requires an authenticated principal, or the response is **401**
  with body `Unauthorized` (**403** when a metadata role is missing). Command and
  query endpoints are marked anonymous only through Arc's `[AllowAnonymous]`
  (`IsAnonymousAllowed`, falling back to the declaring type).

Pipeline authorization (roles, policies, fallback baseline) is a second,
separate check.

### ASP.NET Core hosting (the `Arc` package)

Arc's endpoint mapper marks endpoints anonymous with `AllowAnonymous()`, and it
**does** call `RequireAuthorization(...)` when an endpoint's metadata requires
authentication or roles (for example the protected introspection catalog).
Command and query endpoints are otherwise not protected by the mapper, so
**without a `FallbackPolicy`, ASP.NET Core endpoints are anonymous**, and what
remains is Arc's pipeline authorization on the declared attributes and any
`IFallbackAuthorizationEvaluator`. Decide this deliberately; do not assume "Arc
protects it".

## The filters and results

| Interface | Namespace | Shape |
| --- | --- | --- |
| `IAuthorizationCommandFilter` | `Cratis.Arc.Commands` | Empty marker over `ICommandFilter` (`Task<CommandResult> OnExecution(CommandContext)`) |
| `IAuthorizationQueryFilter` | `Cratis.Arc.Queries` | Empty marker over `IQueryFilter` (`Task<QueryResult> OnPerform(QueryContext)`) |

Both are found by discovery. The marker guarantees ordering: authorization filters
sort first (stable), so a validation failure never short-circuits before the
authorization verdict.

The built-in filters run roles, policies, schemes and the fallback baseline in the
same pipeline. The command filter returns `CommandResult.Unauthorized(context.CorrelationId)`;
the query filter returns `QueryResult.Unauthorized(context.CorrelationId)`, and
returns **success** when no performer is found for the name. A custom filter that
denies uses the same results — not `CommandResult.Error(...)`, which describes an
exception.

Mapped Arc denials set `IsAuthorized = false` and return **403**. Middleware may
reject earlier with **401**, and ASP.NET Core's own challenge/forbid can return a
different shape (a cookie scheme may redirect). Do not infer which layer ran from
the status alone.

⚠️ `AuthorizationResult` — `record (bool IsAuthorized, string? FailureReason)` with
`Success` and `Failure(reason)` — is **not** what either filter returns and not
what `IAuthorizationEvaluator` produces. It is a standalone type, usable as a
command `Provide()` short-circuit value. A handler returning
`ValidationResult.Error(...)` is **validation**, not authorization.

## Direct calls bypass authorization

Attributes do not intercept ordinary C# calls. `new <Command>(...).Handle()` and a
static query method called directly skip authorization, validation, filters and
result handling. Use a mapped endpoint or the command/query pipeline when those
guarantees matter. Server-side callers must still establish a principal (below);
being in-process does not prove permission.

## The principal and system actors

`ICurrentPrincipalAccessor.Current` is a nullable `ClaimsPrincipal`, independent
of transport. During HTTP it is the request principal; outside HTTP it can be the
principal established by a server-side scope. Read claims from it, not from the
identity cookie, and not through `IHttpContextAccessor` in code that must also run
on hub emissions.

Command authorization reads the principal from the current request. A reactor,
hosted service, or one command orchestrating another has no request, so any
command carrying `[Authorize]` or `[Roles]` is **denied**. `ISystemExecution` is
the way out:

```csharp
using var scope = systemExecution.AsSystem(nameof(<RoleEnum>.<Member>));
await pipeline.Execute(new <CommandName>(<args>));
```

- `AsSystem(params string[] roles)` establishes an authenticated system actor for
  the scope. With no roles it satisfies `[Authorize]` but no `[Roles]`.
- `As(ClaimsPrincipal principal)` runs as a specific principal.
- Disposing restores the previous context.

`SystemPrincipal.AuthenticationType` is `"System"` and `SystemPrincipal.Subject`
is `"[System]"`, which is what appears in audit trails. The established principal
is consulted **only** when there is no HTTP request context, so it can never
influence an HTTP-origin command. Keep the scope as narrow as the work: a broad
`AsSystem` around a whole hosted service makes every command it touches
privileged.

## Protected decisions and validators

A `[ProtectedDecision]` command (`Cratis.Arc.Chronicle.ReadModels`) runs only
certified, parameterless validators and **refuses** a validator whose constructor
takes dependencies (a read model, `DecisionRead<T>`, `IDecisionReads`, any
service). So an authorization or ownership rule put in such a validator — or in an
`[Unprotected]` command's validator — is **not decision-safe**. Put
state-dependent permission and ownership rules in `Provide()` or `Handle()` using
`DecisionRead<T>` or `IDecisionReads`, and route to the validation and Chronicle
guidance. Validators that read decision state are tracked in Arc#2831.
