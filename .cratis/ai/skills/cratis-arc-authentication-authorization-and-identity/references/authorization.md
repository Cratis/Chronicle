<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/authorization.md -->
# Authorization

## Contents

- Version requirements
- Attributes
- Composition and precedence
- Named policies
- Guests and anonymous policies
- Evaluation order
- Baselines: two separate mechanisms
- Default access by hosting model
- The filters and results
- Direct calls bypass authorization
- The principal and system actors
- Protected decisions and validators
- Analyzer diagnostics

Source: `Cratis.Arc.Core` and `Cratis.Arc`. Everything below is in `Cratis.Arc.Authorization` unless stated.

## Version requirements

Authorization capabilities arrived over time. Advice that is right for a recent Arc is wrong for an older one, so check the Arc you run (`dotnet list package`).

Requires Arc 22.21.0 for Microsoft's `[Authorize]`/`[AllowAnonymous]` enforcement on the ASP.NET Core host; every stacked requirement applies (AND), and anonymous plus restricted on one member is rejected as ambiguous.

Requires Arc 22.23.0 for named policies and `AuthenticationSchemes` (`IAuthorizationPolicy`, `AddArcAuthorizationPolicy<T>`, ASP.NET Core policies).

Requires Arc 22.26.0 for the `Cratis:Arc:Introspection` options.

Requires Arc 22.30.0 for `IFallbackAuthorizationEvaluator` and `AuthorizationPolicyContext.ReceivedAt`.

Requires Arc 22.31.0 for opt-in policy evaluation for unauthenticated callers.

Requires Arc 22.39.0 for protected decisions.

Requires Arc 22.51.0 for `IdentityDiscovery`, which controls the users/tenants/details schema separately.

On an Arc older than 22.21.0 only Arc's own attributes count, and only `Roles` is evaluated: `Policy` and `AuthenticationSchemes` are read by nothing, so `[Authorize(Policy = "…")]` enforces nothing, and Microsoft's `[Authorize]`/`[AllowAnonymous]` are **not** enforced on model-bound commands and read models (a command protected only with Microsoft's attribute is open to every caller). Either upgrade, or use only Arc's attributes with roles and treat everything else as unenforced. Do not write a policy-protected endpoint on an Arc older than 22.23.0.

Requires Arc 22.31.2 for identity and declaration continuity to be rechecked around policy evaluation.

Requires Arc 22.43.0 for forwarded identity headers to need a host-wide trust opt-in; older versions trust them from any caller.

Requires Arc 22.44.2 for guest-policy endpoints to be reachable over HTTP, including with an authenticated fallback.

Requires Arc 22.45.0 for discovery to be anonymous only in Development; older versions serve it anonymously everywhere.

Requires Arc 22.46.0 for the backend to neither read nor write the identity cookie.

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
(from Arc 22.21.0). The two families can be mixed. Arc's own attributes are
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

Guest-policy endpoints accept unauthenticated callers on both HTTP
hosts, even with an authenticated fallback policy, when every declared policy
opts in. Test through the real HTTP host, not only the in-process verdict.
Source: `Documentation/backend/csharp/introspection/index.md` and the
host authorization specs under `Source/DotNET`.

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

Requires Arc 22.116.0 for hub subscriptions to apply the host's endpoint fallback
before running an unannotated query: ASP.NET Core evaluates `FallbackPolicy`,
and Arc Core requires authentication when authentication handlers are registered.
`IEndpointFallbackAuthorization` supplies this host check; it is separate from
`IFallbackAuthorizationEvaluator`. `[AllowAnonymous]` and guest-policy queries
remain exempt. Denial sends an Unauthorized frame; SSE subscribe returns 401.
Check with `dotnet list package`; on older Arc do not rely on host fallback alone
to secure the hub path.

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

Under read protection `On`, the read models in a convention-registered
`CommandValidator<T>` constructor are read fresh and guard the command's append, so
a state-dependent permission or ownership rule written there is decision-safe. Under
`Off` (the default) and `Measure` the same validator reads a plain model: a hint. In
an `[Unprotected]` command (or a model or method marked `[Unprotected("why")]`) the
read is advisory, so the rule is **not decision-safe**. A `[ProtectedDecision]`
command without `On` runs only certified, parameterless validators and refuses a
validator whose constructor takes dependencies; put such a rule in `Provide()` or
`Handle()` using `DecisionRead<T>` or `IDecisionReads`. Hand-registered scoped,
singleton, factory or instance validators are refused under `On`. Keep
static/service-locator state out of validators: a guard covers reads Arc can see.
Source: [protected validator boundary](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/chronicle/read-models/injecting-into-commands.md).

## Analyzer diagnostics

`ARC0011` warns when a `[Roles]` argument is a string literal: use
`nameof(<RoleEnum>.<Member>)` so a rename is a compile error rather than a silent
authorization failure. `ARC0019` flags `[AllowAnonymous]` with `[Authorize]`/`[Roles]`,
`ARC0020` flags an ASP.NET Core attribute that is not enforced without Arc's ASP.NET
Core integration, and `ARC0021` flags `AuthenticationSchemes` on the Core host.
