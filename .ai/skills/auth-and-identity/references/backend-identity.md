# Backend Identity Provider

This reference covers implementing `IProvideIdentityDetails` to enrich user identity with application-specific details, and what you can use it for.

## What It Is

`IProvideIdentityDetails` is the single extension point where you transform raw authentication claims into rich, application-specific user information. The object you return becomes the `details` field in the frontend identity — available via `useIdentity<TDetails>().details` in React.

Think of it as the bridge between "who the identity provider says this person is" (claims) and "what our application knows about this person" (department, permissions, preferences, tenant, avatar URL, etc.).

## How It Works

When a user hits `GET /.cratis/me`, Arc:

1. Extracts claims from the authenticated `ClaimsPrincipal` (`sub` → `Id`, `Identity.Name` → `Name`, all claims → `Claims`)
2. Builds an `IdentityProviderContext` with user ID, name, and claims
3. Calls your `IProvideIdentityDetails.Provide()` method
4. Serializes the result as JSON and sets the `.cratis-identity` cookie (base64-encoded, `HttpOnly=false` so frontend JS can read it)
5. Returns the result as JSON in the HTTP response

The `Details` object you return can be **any shape** — it gets serialized as JSON. The frontend deserializes it into the `TDetails` type parameter you specify.

## Mapping the Endpoint

In your application startup:

```csharp
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapIdentityProvider();
```

This registers `GET /.cratis/me` — the well-known route the frontend calls to get identity information.

## The Interface

```csharp
// Base interface — single method
public interface IProvideIdentityDetails
{
    Task<IdentityDetails> Provide(IdentityProviderContext context);
}

// Generic variant — adds no methods, just captures TDetails for proxy generation
public interface IProvideIdentityDetails<TDetails> : IProvideIdentityDetails
    where TDetails : class;
```

Always prefer `IProvideIdentityDetails<TDetails>` — the generic type parameter enables automatic TypeScript proxy generation for your details type.

## Basic Implementation

The simplest implementation extracts claims and returns a details record:

```csharp
public record UserDetails(string Department, string Title, bool IsManager);

public class IdentityDetailsProvider : IProvideIdentityDetails<UserDetails>
{
    public Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var department = context.Claims
            .FirstOrDefault(c => c.Key == "department").Value ?? "Unknown";

        var details = new UserDetails(department, "Engineer", false);
        return Task.FromResult(new IdentityDetails(true, details));
    }
}
```

### Rules

- Only **one** `IProvideIdentityDetails` implementation is allowed per app. Multiple implementations throw `MultipleIdentityDetailsProvidersFound`.
- If none exists, `DefaultIdentityDetailsProvider` is used (grants access to everyone with empty details).
- Dependency injection works — your provider can take constructor dependencies.
- The provider is registered as **scoped** (per-request), so it can safely use scoped services.
- Use `IProvideIdentityDetails<TDetails>` (generic) to enable automatic TypeScript proxy generation for the details type. Always prefer the generic interface.

## `IdentityProviderContext`

This is the input to your `Provide()` method — everything Arc knows about the user from authentication:

```csharp
public record IdentityProviderContext(
    IdentityId Id,
    IdentityName Name,
    IEnumerable<KeyValuePair<string, string>> Claims);
```

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `IdentityId` | User's unique ID (extracted from the `sub` claim) |
| `Name` | `IdentityName` | Display name (from `ClaimsPrincipal.Identity.Name`) |
| `Claims` | `IEnumerable<KeyValuePair<string, string>>` | All claims from the authentication token as key-value pairs |

`IdentityId` and `IdentityName` are `ConceptAs<string>` types with `.Empty` sentinels.

### Working with Claims

Claims contain everything the authentication handler extracted from the token. Common claim keys:

| Claim Key | Description | Example Value |
|-----------|-------------|---------------|
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | User ID | `abc123` |
| `name` | Display name | `Jane Doe` |
| `email` or `preferred_username` | Email | `jane@contoso.com` |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | Role | `Admin` |
| Custom claims | Anything your IdP adds | `department`, `tenant_id`, etc. |

```csharp
// Extract a specific claim
var email = context.Claims.FirstOrDefault(c => c.Key == "preferred_username").Value;

// Extract all roles
var roles = context.Claims
    .Where(c => c.Key == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
    .Select(c => c.Value);

// Check if a claim exists
var hasDepartment = context.Claims.Any(c => c.Key == "department");
```

## `IdentityDetails`

The return type from your `Provide()` method:

```csharp
public record IdentityDetails(bool IsUserAuthorized, object Details);
```

| Property | Type | Description |
|----------|------|-------------|
| `IsUserAuthorized` | `bool` | Whether the user is allowed into the application. `false` → HTTP 403. |
| `Details` | `object` | Application-specific details payload (serialized as JSON) |

## What You Can Use It For

### 1. Enriching Identity from a Database

The most common use case — look up the user in your own database and add application-specific information:

```csharp
public record UserDetails(
    UserId UserId,
    string Department,
    string Title,
    string AvatarUrl,
    bool IsManager);

public class IdentityDetailsProvider(IMongoCollection<User> users) : IProvideIdentityDetails<UserDetails>
{
    public async Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var user = await users.Find(u => u.ExternalId == context.Id.Value).FirstOrDefaultAsync();

        if (user is null)
        {
            // Unknown user — still allow access, but with empty details
            return new IdentityDetails(true, new UserDetails(
                UserId.Empty, "Unknown", "Unknown", string.Empty, false));
        }

        var details = new UserDetails(
            user.Id,
            user.Department,
            user.Title,
            user.AvatarUrl,
            user.IsManager);

        return new IdentityDetails(true, details);
    }
}
```

### 2. Blocking Unauthorized Users (Application-Level Gate)

Return `IsUserAuthorized = false` to completely block a user from the application. The endpoint returns HTTP 403 and the frontend never gets identity. This is different from role-based authorization — it's an application-level gate:

```csharp
public class IdentityDetailsProvider(IMongoCollection<AllowedUser> allowedUsers) : IProvideIdentityDetails<UserDetails>
{
    public async Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var isAllowed = await allowedUsers
            .Find(u => u.ExternalId == context.Id.Value)
            .AnyAsync();

        if (!isAllowed)
        {
            // User is authenticated but not authorized for this application
            return new IdentityDetails(false, new UserDetails());
        }

        // ... build details normally
        return new IdentityDetails(true, details);
    }
}
```

Use cases:
- Invite-only applications (only registered users can access)
- Disabled/suspended user accounts
- Tenant-specific access control

### 3. Multi-Tenant Identity

Add tenant information to identity so the frontend knows which tenant context the user is in:

```csharp
public record TenantUserDetails(
    TenantId TenantId,
    TenantName TenantName,
    string Role,
    IEnumerable<TenantId> AvailableTenants);

public class IdentityDetailsProvider(ITenantService tenants) : IProvideIdentityDetails<TenantUserDetails>
{
    public async Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var userTenants = await tenants.GetTenantsForUser(context.Id.Value);
        var activeTenant = userTenants.FirstOrDefault();

        if (activeTenant is null)
        {
            return new IdentityDetails(false, new TenantUserDetails(
                TenantId.Empty, TenantName.Empty, string.Empty, []));
        }

        var details = new TenantUserDetails(
            activeTenant.Id,
            activeTenant.Name,
            activeTenant.UserRole,
            userTenants.Select(t => t.Id));

        return new IdentityDetails(true, details);
    }
}
```

### 4. Enriching From External APIs

Call external services during identity resolution:

```csharp
public class IdentityDetailsProvider(
    IMongoCollection<User> users,
    IGraphApiClient graphClient) : IProvideIdentityDetails<UserDetails>
{
    public async Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        // Combine data from your database and Microsoft Graph
        var user = await users.Find(u => u.ExternalId == context.Id.Value).FirstOrDefaultAsync();
        var graphProfile = await graphClient.GetUserProfile(context.Id.Value);

        var details = new UserDetails(
            user?.Department ?? graphProfile.Department,
            graphProfile.JobTitle,
            graphProfile.Photo);

        return new IdentityDetails(true, details);
    }
}
```

### 5. Claims-Only Provider (No Database)

Sometimes all you need is to reshape token claims into a application-friendly structure — no database lookup required:

```csharp
public record UserDetails(string Email, string Department, IEnumerable<string> Groups);

public class IdentityDetailsProvider : IProvideIdentityDetails<UserDetails>
{
    public Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var email = context.Claims.FirstOrDefault(c => c.Key == "preferred_username").Value ?? string.Empty;
        var department = context.Claims.FirstOrDefault(c => c.Key == "department").Value ?? "Unknown";
        var groups = context.Claims
            .Where(c => c.Key == "groups")
            .Select(c => c.Value);

        return Task.FromResult(new IdentityDetails(
            true,
            new UserDetails(email, department, groups)));
    }
}
```

### 6. User Preferences and Settings

Surface user preferences (theme, language, selected workspace) as identity details so the frontend can use them immediately on load:

```csharp
public record UserDetails(
    string Theme,
    string Language,
    WorkspaceId ActiveWorkspace);

public class IdentityDetailsProvider(IMongoCollection<UserPreferences> preferences) : IProvideIdentityDetails<UserDetails>
{
    public async Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var prefs = await preferences
            .Find(p => p.UserId == context.Id.Value)
            .FirstOrDefaultAsync();

        var details = new UserDetails(
            prefs?.Theme ?? "light",
            prefs?.Language ?? "en",
            prefs?.ActiveWorkspace ?? WorkspaceId.Empty);

        return new IdentityDetails(true, details);
    }
}
```

## Modifying Identity at Runtime

`IIdentityProviderResultHandler` allows modifying identity details during requests — useful for stateless applications tracking user selections without a database round-trip:

```csharp
public class SetActiveWorkspace(IIdentityProviderResultHandler identityHandler)
{
    public async Task Handle(WorkspaceId workspaceId) =>
        await identityHandler.ModifyDetails<UserDetails>(
            details => details with { ActiveWorkspace = workspaceId });
}
```

This re-generates the identity from the current context, applies the modifier function to the details, and re-writes the cookie. The frontend will see the updated details on the next `identity.refresh()` call or page load.

| Method | Description |
|--------|-------------|
| `GenerateFromCurrentContext()` | Build `IdentityProviderResult` from current HTTP context |
| `Write(result)` | Write result as JSON response + update cookie |
| `ModifyDetails<T>(modifier)` | Read current cookie, apply modifier, write updated cookie |

### When to Use `ModifyDetails` vs. Database Updates

| Approach | Use When |
|----------|----------|
| `ModifyDetails<T>()` | Ephemeral selections (active tenant, selected workspace, UI preferences) that don't need persistence. Updates the cookie in-place. |
| Database update + `identity.refresh()` | Persistent changes (profile updates, role changes). Update the database, then have the frontend call `refresh()` to pick up the new data. |

## Proxy Generation for Identity Types

When using the generic `IProvideIdentityDetails<TDetails>`, the proxy generator automatically creates TypeScript types for `TDetails` at build time:

1. Define a C# record for your details type
2. Implement `IProvideIdentityDetails<YourDetails>`
3. Run `dotnet build` — TypeScript interface is generated
4. Import and use the generated type in your frontend

| Feature | `IProvideIdentityDetails` | `IProvideIdentityDetails<TDetails>` |
|---------|---------------------------|-------------------------------------|
| Runtime behavior | Identical | Identical |
| Proxy generation | No type generated | TypeScript type generated |
| Frontend type safety | Manual typing required | Automatic type safety |

## Default Behavior (No Provider)

If you don't implement `IProvideIdentityDetails`, Arc uses `DefaultIdentityDetailsProvider`:

```csharp
public class DefaultIdentityDetailsProvider : IProvideIdentityDetails
{
    public Task<IdentityDetails> Provide(IdentityProviderContext context) =>
        Task.FromResult(new IdentityDetails(true, new { }));
}
```

This grants access to everyone with empty details. It's useful for apps that only need authentication (roles) without custom details.

## Multi-Service Considerations

- **Single service**: Implement `IProvideIdentityDetails` directly
- **Multiple services**: Have your ingress/reverse proxy call each service's `/.cratis/me` and merge results into a single JSON structure for the `.cratis-identity` cookie
- **Dedicated identity service**: Aggregate identity info from various sources in one service
