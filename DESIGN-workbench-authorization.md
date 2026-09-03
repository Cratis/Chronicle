# Workbench Authorization and Security Architecture

**Status:** proposed  
**Profile:** Chronicle framework  
**Related:** `PLAN-workbench-product.md`, `WORKBENCH-OPERATOR-ACTION-INVENTORY.md`,
`DESIGN-workbench-operator-actions.md`

---

## 1. Decision summary

Chronicle will separate authentication, coarse roles, fine permissions, and resource scope:

1. **Authentication** proves the caller identity.
2. **Role** describes a job function and grants a permission set.
3. **Permission** grants one class of action.
4. **Resource scope** limits the event stores, namespaces, and resources on which the permission can
   be exercised.

The server is the security boundary. Workbench permission checks are presentation only.

New enveloped operator actions fail closed when no stable actor or authorization decision is
available. Existing endpoints migrate one at a time after role and scope assignments can be issued
to both internal users and external principals.

---

## 2. Current evidence

- `Source/Kernel/Server/Authentication/ServiceCollectionExtensions.cs` configures a fallback policy
  requiring an authenticated principal when authentication is enabled.
- When authentication is disabled, the fallback policy permits requests.
- `Source/Kernel/Server/Authentication/ChronicleClaimsTransformation.cs` adds `sub` and `name`, but no
  Chronicle roles, permissions, or resource scopes.
- Internal `Source/Kernel/Core/Security/User.cs` has no role or scope state.
- Generated Workbench proxies contain no roles.
- Operation-specific authorization was not found on the current Workbench mutation surface.
- `ChangePasswordForUser` is anonymous, but its Kernel handler validates the current password. It is
  a self-service/first-login concern, not an arbitrary administrative reset.
- At the committed baseline, cookie authentication uses `CookieSecurePolicy.SameAsRequest`; the
  uncommitted candidate requires Secure cookies.
- At the committed baseline, `IdentityConverters.ToContract()` generates a random subject when an
  authenticated principal has no `sub`; the uncommitted candidate rejects missing/blank subjects.
- At the committed baseline, internal-authority token validation disables audience validation in
  `Authentication/OpenIddict/OpenIddictServiceCollectionExtensions.cs`; the uncommitted candidate
  issues and validates a stable Chronicle audience with focused specifications.

The absence of permissions is the critical defect. The cookie, actor, password-flow, CSRF, and token
validation findings are immediate security-hardening prerequisites for new mutation surfaces. Authentication alone does not distinguish a
reader, operator, data steward, security administrator, or service principal.

---

## 3. Threat model

Protect against:

- an authenticated reader invoking replay, redaction, reset, or user-management operations;
- a client-credentials principal exercising human operator actions;
- cross-event-store or cross-namespace access;
- UI-only enforcement bypassed through HTTP, gRPC, CLI, or MCP;
- forged actor identifiers supplied in requests;
- cookie-authenticated cross-site request forgery;
- role/scope claim confusion between external identity providers;
- silent permission fallback when claims are missing or malformed;
- unaudited denied attempts and break-glass access;
- audit records containing passwords, tokens, event payloads, secrets, or unnecessary PII.

---

## 4. Roles

Initial role vocabulary:

| Role | Purpose |
|---|---|
| `Chronicle.Reader` | Browse explicitly permitted operational and event data; no mutations |
| `Chronicle.Operator` | Retry/replay and manage operational Jobs within assigned scopes |
| `Chronicle.DataSteward` | Append, revise, redact, and export data within assigned scopes |
| `Chronicle.SecurityAdministrator` | Manage users, applications, role assignments, and audit access |
| `Chronicle.Administrator` | All Chronicle permissions and scopes |

Roles are convenience bundles, not the authorization check itself. The check is permission plus
resource scope.

Readers do not automatically receive sensitive audit, application event content, compliance, or
security data. Protected sequences/event types remain excluded even when application event-content
permission is present.

---

## 5. Permissions

Initial permission vocabulary:

### Read permissions

- `chronicle.operations.read`
- `chronicle.events.metadata.read`
- `chronicle.events.content.read`
- `chronicle.audit.read`
- `chronicle.security.read`

`chronicle.events.content.read` covers application event content only. It never grants action-history,
security/authentication, credential, key, or other protected sequence/event content. Audit permission
exposes the sanitized action-history read model, not raw protected events. Security permission exposes
security administration/metadata, not password hashes, tokens, or credential-event payloads.

### Observation and Job permissions

- `chronicle.failed-partition.retry`
- `chronicle.observer.quarantine.clear`
- `chronicle.observer.partition.replay`
- `chronicle.observer.replay`
- `chronicle.job.stop`
- `chronicle.job.resume`
- `chronicle.job.delete`
- `chronicle.recommendation.perform`
- `chronicle.recommendation.ignore`

### Data permissions

- `chronicle.event.append`
- `chronicle.event.revise`
- `chronicle.event.redact`
- `chronicle.event.export`
- `chronicle.seed.manage`

### Configuration, saved-view, and security permissions

- `chronicle.event-store.manage`
- `chronicle.namespace.manage`
- `chronicle.event-type.manage`
- `chronicle.read-model-type.manage`
- `chronicle.sequence-query.manage-own`
- `chronicle.sequence-query.manage-shared`
- `chronicle.projection.manage`
- `chronicle.capture.manage`
- `chronicle.webhook.manage`
- `chronicle.external-service.manage`
- `chronicle.security.user.manage`
- `chronicle.security.application.manage`
- `chronicle.security.role.manage`
- `chronicle.development.kernel-reset`

Permission names are public API. They are additive and never silently repurposed.

---

## 6. Resource scopes

Resource authorization follows ASP.NET Core resource-based authorization: load/normalize the target,
then authorize the principal against the permission and resource.

Scopes form a hierarchy:

```text
System
└── Event store
    └── Namespace
        ├── Observer
        │   └── Partition
        ├── Event sequence
        │   └── Event
        ├── Job
        └── Recommendation
```

Saved-query ownership is part of authorization, not only event-store scope:

- user-scoped queries/folders can be changed only by their owner or a security administrator;
- shared queries/folders require `chronicle.sequence-query.manage-shared`;
- delete/update loads the saved definition first and authorizes against its owner and scope;
- request-supplied owner/scope is never trusted for an existing definition.

A scope assignment contains:

- role or explicit permission;
- event-store selector;
- namespace selector;
- optional resource selector;
- validity window;
- assignment source.

Wildcard scope is represented explicitly, never by an empty string. `NotSet` and `Default` remain
distinct.

Every negative cross-namespace and cross-event-store case requires a specification.

---

## 7. Enforcement architecture

### 7.1 Boundary checks

- Arc model-bound commands and queries use Cratis Arc authorization attributes for coarse role
  metadata and generated-UI hints.
- MVC endpoints use ASP.NET Core policies.
- Both delegate fine resource authorization to a shared `IOperatorAuthorizationService`.
- The operation coordinator calls the same service again before execution, so direct gRPC and
  in-process callers cannot bypass HTTP checks.

The authorization result is structured:

```text
Allowed | Denied | Indeterminate
```

`Indeterminate` fails closed. It is used when actor identity, permission mapping, target scope, or
authorization storage cannot be resolved.

### 7.2 Resource-based handler

Add an `OperatorActionRequirement` and handler over an `OperatorActionResource` containing the
normalized event store, namespace, resource kind, resource id, and action kind.

Attributes alone cannot express this because they run before target resources are resolved.

### 7.3 Shadow mode

Before enforcement:

1. evaluate every current action;
2. emit structured allow/deny/indeterminate telemetry;
3. do not change behavior;
4. measure which users/applications would be blocked;
5. assign roles/scopes;
6. enable enforcement per action family.

Shadow mode has an explicit expiry. It must not become permanent authorization theater.

---

## 8. Internal-user migration

Do not add one mutable `Role` string to the user record. Add separate past-tense events:

- `UserRoleAssigned`
- `UserRoleRevoked`
- `UserResourceScopeGranted`
- `UserResourceScopeRevoked`

Project them into role and scope collections on the internal user read model.

Migration sequence:

1. Bootstrap administrator receives `Chronicle.Administrator`.
2. Existing active users are reported as unassigned during shadow mode.
3. An administrator assigns roles/scopes explicitly.
4. Enforcement cannot be enabled while active internal users remain unassigned, unless the owner
   explicitly chooses a compatibility default.
5. New users require role/scope selection or default to no permissions.

Do not silently grant every historical user administrator forever. If compatibility requires a
temporary grant, persist it as an explicit migration assignment with an expiry and audit record.

`ChronicleClaimsTransformation` emits role and permission claims from the projected assignments.

---

## 9. External JWT/OIDC and client credentials

Configuration maps trusted issuer claims to Chronicle roles and scopes. Mapping is issuer-specific;
claims from an unrecognized issuer are never interpreted as Chronicle permissions.

Requirements:

- validate issuer, audience, signature, lifetime, and authorized party/client id;
- require stable issuer plus subject;
- distinguish human users from service principals;
- prevent service principals from receiving human-only permissions unless explicitly configured;
- normalize role arrays from the configured claim type;
- cap claim and scope cardinality;
- reject malformed wildcards and selectors;
- record mapping version in authorization telemetry.

The uncommitted candidate resolves the internal-authority audience TODO and configures external
JwtBearer authority, audience, and HTTPS metadata with specifications. Issuer-specific role/scope
claim mapping and authorized-party policy remain before enterprise rollout.

---

## 10. Stable actor identity

Operator action requests never contain an actor id.

The server resolves a stable `sub` and issuer from the authenticated principal. If no stable subject
exists, the operation is rejected.

The durable action event uses Chronicle's event context `CausedBy` identity chain rather than
copying an actor name, email, or username into every event. Operator-action read models store the
resulting `IdentityId`; authorized UI queries resolve display details separately.

Remove random-subject fallback from the new operation path. Also replace the existing
`IdentityConverters.ToContract()` random GUID behavior with an explicit stable-identity failure for
mutating event operations, through a compatibility-reviewed change. A random stable-looking actor
poisons causation and action history; it must not survive as an alternate mutation path.

---

## 11. Authentication-disabled deployments

Authentication-disabled mode is not an authorization model.

Define three cumulative deployment modes:

- **Authenticated:** reads and mutations require identity, permission, and resource scope.
- **Anonymous read-only:** approved queries may be anonymous; **all current and new mutations are
  denied**, not only operator-action endpoints.
- **Unsafe development:** explicit opt-in, Development environment, local binding, persistent warning,
  synthetic actor, and unavailable in production builds.

Production startup refuses any mutation-enabled configuration without authentication. Proxy or
forwarded-address assumptions are not substitutes for authentication.

Migrating all existing mutation endpoints to these modes is behavior-breaking and needs an explicit
semver/release plan, but it is on the critical path. The 48 existing commands cannot remain an
indefinite bypass while only new endpoints are secured.

---

## 12. CSRF

At the committed baseline, cookie authentication uses `HttpOnly`, `SameSite=Lax`,
`SecurePolicy.SameAsRequest`, and sliding expiration. The uncommitted candidate requires Secure
cookies and adds antiforgery issuance plus validation for authenticated cookie mutations. Trusted
forwarded-header configuration and development HTTP behavior remain explicit.

Candidate design:

- authenticated same-origin endpoint issues an antiforgery request token;
- Arc command/query/SSE-control transport sends it through the shared HTTP-header callback;
- raw Workbench logout sends the same header;
- server validates state-changing authenticated cookie requests;
- bearer-token calls and explicitly anonymous bootstrap/authentication endpoints are unaffected;
- Origin/Referer checks are defense in depth, not the primary token;
- SameSite and Secure cookies remain defense in depth and must not be treated as complete CSRF
  protection.

Unit specifications cover cookie/bearer/anonymous middleware paths and Workbench token handling.
The uncommitted candidate also removes group-wide `.AllowAnonymous()` from `MapIdentityApi`, permits
only login and refresh anonymously, protects registration/management through the fallback policy,
and adds an authenticated logout endpoint.

Out-of-process browser integration must still cover missing, invalid, expired, cross-origin, login,
logout, registration denial, management authorization, SSE subscribe/unsubscribe, and Arc command
requests before release.

---

## 13. Password-change endpoint

`ChangePasswordForUser` verifies the old password in
`Source/Kernel/Core/Security/ChangeUserPassword.cs`. Keep the self-service behavior separate from
administrative reset.

Required hardening:

- rate-limit by source and target;
- return non-enumerating failures;
- apply CSRF protection when cookie authenticated;
- log structured success/failure security events without passwords;
- require a short-lived reset token for forgotten-password flows;
- create a separate administrator reset action protected by
  `chronicle.security.user.manage`.

Do not simply replace `[AllowAnonymous]` with an administrator role; that would break legitimate
first-login/self-service behavior without providing a replacement.

### Initial-password bootstrap

`InitialAdminPasswordSetupStatus` currently discloses a user id, and `SetInitialAdminPassword`
accepts a caller-supplied user id while the Kernel checks only `HasLoggedIn == false`. The name does
not enforce that the target is the administrator.

Replace this with a bootstrap claim:

- status reports only whether bootstrap is required, not a target user id;
- startup creates a short-lived, single-use, securely stored bootstrap token bound to the exact
  bootstrap administrator identity and deployment;
- initial-password submission carries that token, not an arbitrary user id;
- token validation, consumption, and password write are one guarded operation;
- rate limiting and non-enumerating failures apply;
- successful or denied bootstrap attempts produce sanitized security telemetry;
- bootstrap is permanently disabled once consumed unless an explicit host-level recovery procedure
  re-enables it.

This is a P0 security prerequisite, independent of the operator-action pilot.

---

## 14. Protected event content

Current security events such as `UserPasswordChanged` carry password hashes in the ordinary System
event store log. Generic event browsing/export must classify and suppress protected event content
before the permission model is enforced.

Required work:

- mark protected sequences/event types in server metadata;
- deny generic query/export/content access regardless of application-content permission;
- expose only purpose-built sanitized security/audit read models;
- verify projections, saved queries, CLI, MCP, and direct gRPC cannot bypass the classification;
- decide whether credential material should remain event payload at all in a separate security design.

## 15. Denied attempts and security telemetry

Authorized operation lifecycle events belong in the durable operator ledger. Denied and malformed
requests belong in high-signal security telemetry, not necessarily the target namespace ledger;
otherwise attackers can create unbounded durable events.

Emit stable OpenTelemetry event names with dynamic data in attributes:

- `cratis.chronicle.operator_action.authorization_denied`
- `cratis.chronicle.operator_action.authorization_indeterminate`
- `cratis.chronicle.operator_action.request_rejected`
- `cratis.chronicle.authentication.password_change_failed`

Attributes include actor identity id, action kind, resource kind, event-store/namespace scope,
reason code, decision policy version, correlation id, and outcome. Do not emit passwords, tokens,
request bodies, event content, connection strings, secrets, or raw stack traces.

OWASP's “when, where, who, what, result, reason” model is the minimum. Time is UTC and all log access
is itself restricted and monitored.

---

## 16. Break-glass access

Do not add an anonymous Workbench endpoint accepting a shared emergency token.

Preferred order:

1. external identity-provider break-glass account with strong controls;
2. infrastructure-level short-lived client certificate or token;
3. local recovery CLI available only on the Kernel host;
4. two-person approval where the hosting environment supports it.

Any break-glass identity is short-lived, explicitly marked, narrowly scoped where possible, and
generates elevated security telemetry and durable records for authorized actions.

Detailed break-glass implementation is deferred until the deployment model and secret authority are
owned explicitly.

---

## 17. Least-privilege Workbench UX

Expose an effective-capabilities query for the current principal and selected event store/namespace.
Workbench uses it to:

- hide unavailable navigation;
- disable actions with an explanation when context is useful;
- show required permission and scope;
- distinguish denied from unavailable and unsupported;
- never infer permission from a missing role array.

The server always repeats authorization.

---

## 18. Rollout and acceptance gates

1. Harden stable actor resolution, production cookie security, password flows/CSRF, and internal
   token audience validation.
2. Define public role, permission, resource, and decision contracts.
3. Add internal role/scope events and projections.
4. Add issuer-specific external claim mapping.
5. Add shadow authorization and decision telemetry.
6. Add effective-capabilities query and least-privilege UI.
7. Enforce the failed-partition retry pilot.
8. Migrate one action family per PR.
9. Remove legacy bypasses only in a semver-compatible release plan.

Non-negotiable tests:

- unauthenticated, wrong role, missing permission, wrong event store, wrong namespace;
- internal user and external principal mappings;
- service-principal restrictions;
- authentication-disabled production startup;
- CSRF cookie/bearer split;
- stable actor identity and no random fallback;
- no secrets/PII in decision telemetry;
- shadow/enforced policy equivalence;
- every generated proxy advertises the server-required coarse role metadata.

---

## 19. Owner decisions

- External claim names and issuer mapping format.
- Compatibility treatment for existing internal users.
- Whether event-content read is separate from operational metadata read.
- Whether application principals may mutate or remain read/automation-only.
- Scope wildcard semantics.
- Retention and access policy for authorization/security telemetry.
- Production behavior when authentication is disabled.
