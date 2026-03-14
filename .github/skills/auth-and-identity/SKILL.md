---
name: auth-and-identity
description: Use this skill when asked to set up authentication, authorization, or identity in a Cratis Arc project — backend, frontend, or both. Covers implementing identity providers, protecting commands/queries with authorization attributes, integrating Microsoft Identity Platform, connecting backend identity to React frontends, and local development testing with generated principals. Also covers customizing IProvideIdentityDetails for enriching identity from databases, blocking users, multi-tenant identity, user preferences, and modifying identity at runtime. Use this whenever the user mentions auth, login, roles, permissions, identity details, user context, protecting endpoints, identity provider customization, or anything related to who the user is and what they can access.
---

# Auth & Identity in Cratis Arc

This skill covers the full auth and identity stack in a Cratis Arc application. Read the relevant reference files below for detailed API usage.

> **Read the relevant instruction files first.** This skill references concepts from the core copilot instructions in `.github/copilot-instructions.md`. If you need details on vertical slices, commands, queries, or proxy generation, consult those instructions.

## Architecture Overview

Identity and auth in Arc follow a cookie-first, convention-based pattern:

```
Frontend (React)                          Backend (ASP.NET Core)
─────────────────                         ──────────────────────
<IdentityProvider>                        app.MapIdentityProvider()
  └── useIdentity() hook                    └── GET /.cratis/me
        │                                        │
        ├─ 1. Read .cratis-identity cookie        │
        └─ 2. If no cookie → fetch /.cratis/me    │
                                                  ▼
                                    AuthenticationMiddleware
                                      └── IAuthenticationHandler[]
                                            │ sets HttpContext.User
                                            ▼
                                    IIdentityProviderResultHandler
                                      └── IProvideIdentityDetails.Provide()
                                            │
                                            ▼
                                    IdentityProviderResult
                                      → JSON response
                                      → .cratis-identity cookie (base64)

Authorization:
  [Authorize] / [Roles("Admin")] / [AllowAnonymous]
    └── AuthorizationEvaluator checks per command/query
```

**Key design decisions:**
- The `.cratis-identity` cookie is `HttpOnly=false` so the frontend JavaScript can read it directly — no extra HTTP call needed on page load.
- Identity details are base64-encoded JSON in the cookie, automatically decoded by the frontend `IdentityProvider`.
- Only one `IProvideIdentityDetails` implementation is allowed per application (auto-discovered). If none exists, a default provider grants access to everyone.
- Cratis has its own `[Authorize]`, `[AllowAnonymous]`, and `[Roles]` attributes in `Cratis.Arc.Authorization` — these are distinct from ASP.NET Core's and are evaluated by `AuthorizationEvaluator` in the command and query pipeline.

---

## Decision Tree — Which Reference to Read

Use this decision tree to determine which reference file(s) to read based on the user's task:

| User wants to... | Read this reference |
|---|---|
| Add identity details to their app | [references/backend-identity.md](references/backend-identity.md) |
| Customize `IProvideIdentityDetails` (enrich from DB, block users, multi-tenant, preferences) | [references/backend-identity.md](references/backend-identity.md) |
| Modify identity at runtime (stateless selections, `ModifyDetails`) | [references/backend-identity.md](references/backend-identity.md) |
| Use Azure AD / Entra ID / Microsoft Identity | [references/authentication.md](references/authentication.md) |
| Write a custom authentication handler (API key, JWT, etc.) | [references/authentication.md](references/authentication.md) |
| Protect commands or queries with roles | [references/authorization.md](references/authorization.md) |
| Set up `[Authorize]`, `[AllowAnonymous]`, or `[Roles]` | [references/authorization.md](references/authorization.md) |
| Consume identity in React | [references/frontend.md](references/frontend.md) |
| Use identity in MVVM or vanilla TypeScript | [references/frontend.md](references/frontend.md) |
| Test identity locally without Azure | [references/local-development.md](references/local-development.md) |
| Full-stack setup (backend + frontend) | Read all references in order |

**For full-stack tasks, read in this order:**
1. `references/backend-identity.md` — identity provider and startup
2. `references/authentication.md` — how users are authenticated
3. `references/authorization.md` — protecting commands and queries
4. `references/frontend.md` — consuming identity in the UI
5. `references/local-development.md` — testing without real infrastructure

---

## Quick-Start: Full-Stack Identity Setup

This is the minimum checklist for an application with identity. Read the reference files for details on each step.

### Backend

1. **Details record**: Define a C# record for application-specific user information
2. **Identity provider**: Implement `IProvideIdentityDetails<TDetails>` (auto-discovered, one per app)
3. **Startup**: Call `app.MapIdentityProvider()` to register `GET /.cratis/me`
4. **Authentication**: Add `AddMicrosoftIdentityPlatformIdentityAuthentication()` or implement `IAuthenticationHandler`
5. **Authorization**: Add `[Authorize]` / `[Roles]` / `[AllowAnonymous]` attributes from `Cratis.Arc.Authorization` to commands and queries

### Frontend

6. **Provider**: Wrap app root with `<IdentityProvider>` from `@cratis/arc.react/identity`
7. **Hook**: Use `useIdentity<TDetails>()` to access identity anywhere in the component tree
8. **Roles**: Use `identity.isInRole('Admin')` for UI-level role gating

### Proxy Generation

9. Using `IProvideIdentityDetails<TDetails>` (generic) enables automatic TypeScript type generation at `dotnet build` time — the generated types can be imported in the frontend for end-to-end type safety.

---

## Critical Rules

These rules are frequently violated — always enforce them:

1. **One identity provider per app**: Only one `IProvideIdentityDetails` implementation is allowed. Multiple throws `MultipleIdentityDetailsProvidersFound`.
2. **Use Cratis attributes, not ASP.NET Core's**: `[Authorize]`, `[Roles]`, `[AllowAnonymous]` must come from `Cratis.Arc.Authorization`, not `Microsoft.AspNetCore.Authorization`.
3. **Never combine `[Authorize]` and `[AllowAnonymous]` on the same target**: This throws `AmbiguousAuthorizationLevel`.
4. **Prefer the generic interface**: Use `IProvideIdentityDetails<TDetails>` over `IProvideIdentityDetails` to enable proxy generation.
5. **Auto-discovery**: Both `IProvideIdentityDetails` and `IAuthenticationHandler` implementations are auto-discovered — no DI registration needed.
6. **Frontend role checks are UX, not security**: `isInRole()` on the frontend hides UI elements. The backend `[Roles]` attribute is the actual security boundary.
7. **Build before frontend**: TypeScript proxy types for identity details are generated by `dotnet build`. The backend must compile before the frontend can import them.

---

## Common Code Patterns

### Protecting a command with roles

```csharp
[Command]
[Roles("Admin")]
public record PromoteUser(UserId Id)
{
    public void Handle(IUserService users) => users.Promote(Id);
}
```

### Conditionally rendering UI based on roles

```tsx
const identity = useIdentity();

return identity.isInRole('Admin')
    ? <AdminPanel />
    : <AccessDenied />;
```

### Modifying identity at runtime (stateless selections)

```csharp
public class SetDepartment(IIdentityProviderResultHandler identityHandler)
{
    public async Task Handle(string department) =>
        await identityHandler.ModifyDetails<UserDetails>(
            details => details with { SelectedDepartment = department });
}
```

---

## Reference Documentation

### Skill references (detailed implementation guidance)

- [Backend Identity Provider](references/backend-identity.md) — `IProvideIdentityDetails`, `IdentityProviderContext`, cookie mechanics, proxy generation, `ModifyDetails`
- [Authentication](references/authentication.md) — `IAuthenticationHandler`, `AuthenticationResult`, Microsoft Identity Platform, combining handlers
- [Authorization](references/authorization.md) — `[Authorize]`, `[Roles]`, `[AllowAnonymous]`, inheritance rules, fallback policies
- [Frontend Identity](references/frontend.md) — React `IdentityProvider`, `useIdentity()`, MVVM, core identity, role checking
- [Local Development](references/local-development.md) — Generating principals, ModHeader, cookie fallback, dev testing

### Project documentation

- [Backend Identity](Documentation/backend/identity.md)
- [Microsoft Identity](Documentation/backend/asp-net-core/microsoft-identity.md)
- [ASP.NET Core Authorization](Documentation/backend/asp-net-core/authorization.md)
- [Core Authentication](Documentation/backend/core/authentication.md)
- [Core Authorization](Documentation/backend/core/authorization.md)
- [Command Authorization](Documentation/backend/commands/model-bound/authorization.md)
- [Query Authorization](Documentation/backend/queries/model-bound/authorization.md)
- [Proxy Generation for Identity](Documentation/backend/proxy-generation/identity-details.md)
- [Frontend Core Identity](Documentation/frontend/core/identity.md)
- [Frontend React Identity](Documentation/frontend/react/identity.md)
- [Frontend MVVM Identity](Documentation/frontend/react.mvvm/identity.md)
- [Generating Principals for Local Dev](Documentation/general/generating-principal.md)
