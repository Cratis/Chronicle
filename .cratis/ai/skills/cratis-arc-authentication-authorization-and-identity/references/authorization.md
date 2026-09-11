<!-- cratis-ai-managed: skills/cratis-arc-authentication-authorization-and-identity/references/authorization.md -->
# Authorization

Verified against `Cratis.Arc.Core` and `Cratis.Arc` `22.10.4`. Everything below
is in `Cratis.Arc.Authorization` unless stated.

## Default access differs by hosting model

There is **no option anywhere in Arc** that sets a default authentication or
authorization requirement. `ArcOptions` has no such switch. What you get depends
on which pipeline is running.

### Self-hosted `ArcApplication` (Arc.Core)

`AuthenticationMiddleware` runs per request:

- **No `IAuthenticationHandler` registered at all → every endpoint is anonymous.**
  The middleware returns immediately.
- With at least one handler registered, an endpoint marked `AllowAnonymous`
  passes and everything else requires an authenticated principal — otherwise the
  response is **401** with a body of `Unauthorized`.

`AllowsAnonymousAccess` defaults to **false**: `IsAnonymousAllowed()` on a
top-level type with no attributes has no declaring type to fall back to and
returns false. So under this pipeline commands and queries are
authenticated-by-default *once any handler exists*.

### ASP.NET Core hosting (the `Arc` package)

The endpoint mapper only ever calls `AllowAnonymous()`. It **never** calls
`RequireAuthorization()`, and nothing else in Arc does either. Endpoints are
therefore **anonymous by default** unless the application configures its own
fallback policy in ASP.NET Core.

Decide this deliberately for your host. Do not assume "Arc protects it".

## Role evaluation

`AuthorizationEvaluator`:

1. every `IAnonymousEvaluator` is asked; the first non-null answer wins — `true`
   authorizes immediately, `false` stops asking and moves on;
2. every `IAuthorizationAttributeEvaluator` is asked for `[Authorize]` and its
   `Roles`; the first that reports one wins;
3. no `[Authorize]` → **authorized**;
4. `[Authorize]` with a null or unauthenticated principal → denied;
5. `[Authorize]` with roles → authorized when the user is in **any** of them.
   The string is comma-split and trimmed. It is OR, never AND;
6. a `MethodInfo` with no `[Authorize]` falls back to its declaring type.

`[Authorize]` and `[AllowAnonymous]` on the same member throws
`AmbiguousAuthorizationLevel` from `AnonymousEvaluator` and from
`AllowAnonymousExtensions.IsAnonymousAllowed`.

⚠️ The ASP.NET Core-flavoured evaluators (`AspNetAnonymousEvaluator`,
`AspNetAuthorizationAttributeEvaluator`) are declared inside
`namespace Cratis.Arc.Authorization` and do not import
`Microsoft.AspNetCore.Authorization`, so the attribute names in them bind to
**Arc's own** attributes. Do not rely on ASP.NET Core's `[Authorize]` being
honoured by them. `AspNetAnonymousEvaluator` also does not throw on the
both-attributes case; it prefers `AllowAnonymous`.

## The filters

| Interface | Namespace | Shape |
| --- | --- | --- |
| `IAuthorizationCommandFilter` | `Cratis.Arc.Commands` | Empty marker over `ICommandFilter` (`Task<CommandResult> OnExecution(CommandContext)`) |
| `IAuthorizationQueryFilter` | `Cratis.Arc.Queries` | Empty marker over `IQueryFilter` (`Task<QueryResult> OnPerform(QueryContext)`) |

Both are found by discovery — implement one on a public class and it is picked
up. The marker is what guarantees ordering: the filter chains sort
authorization filters first with a stable sort, so a validation failure can never
short-circuit the chain before the authorization verdict is recorded.

Built-in implementations: the command `AuthorizationFilter` returns
`CommandResult.Unauthorized(...)` from `IAuthorizationEvaluator`; the query
`AuthorizationFilter` asks the query performer, and returns **success** when no
performer is found for the name.

⚠️ `AuthorizationResult` — `record (bool IsAuthorized, string? FailureReason)`
with `Success` and `Failure(reason)` — is **not** what either filter returns and
not what `IAuthorizationEvaluator` produces (that returns a plain `bool`). It is
a standalone type, usable as a command `Provide()` short-circuit value. Do not
describe it as the filter contract.

## Executing as a system actor

Command authorization reads the principal from the current HTTP request. A
reactor, hosted service, or one command orchestrating another has no request, so
any command carrying `[Authorize]` or `[Roles]` is **denied**.

`ISystemExecution` is the way out:

```csharp
using var scope = systemExecution.AsSystem(nameof(<RoleEnum>.<Member>));
await pipeline.Execute(new <CommandName>(<args>));
```

- `AsSystem(params string[] roles)` establishes an authenticated system actor for
  the scope. With no roles it satisfies `[Authorize]` but no `[Roles]`.
- `As(ClaimsPrincipal principal)` runs as a specific principal.
- Disposing restores the previous context.

`SystemPrincipal.AuthenticationType` is `"System"` and `SystemPrincipal.Subject`
is `"[System]"`, which is what appears in audit trails.

The established principal is consulted **only** when there is no HTTP request
context, so it can never influence the authorization of an HTTP-origin command.
`CurrentPrincipalAccessor` implements that: a request principal always wins over
the ambient override.

Keep the scope as narrow as the work. A broad `AsSystem` around a whole hosted
service turns every command it touches into a privileged one.
