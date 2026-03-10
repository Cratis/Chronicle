# Authentication

This reference covers implementing custom authentication handlers and integrating Microsoft Identity Platform.

## Authentication Pipeline

Arc's authentication system is built around `IAuthenticationHandler` in `Cratis.Arc.Authentication`. Handlers are called in sequence until one succeeds or fails:

1. Each registered handler is called in order
2. If a handler returns `Succeeded` → request is authenticated, pipeline stops
3. If a handler returns `Failed` → request is rejected (401), pipeline stops
4. If a handler returns `Anonymous` → try the next handler
5. If all handlers return `Anonymous` → request is unauthenticated

## `AuthenticationResult` Outcomes

| Method | Meaning | When to Use |
|--------|---------|-------------|
| `AuthenticationResult.Anonymous` | Handler doesn't apply | No relevant credentials found — let other handlers try |
| `AuthenticationResult.Succeeded(principal)` | Authentication succeeded | Valid credentials → return a `ClaimsPrincipal` |
| `AuthenticationResult.Failed(reason)` | Authentication explicitly failed | Credentials present but invalid → return 401 |

## Custom Authentication Handler

Handlers are **auto-discovered** — implement `IAuthenticationHandler` and Arc registers it. No manual DI wiring needed.

```csharp
using System.Security.Claims;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;

public class ApiKeyAuthenticationHandler(IApiKeyValidator validator) : IAuthenticationHandler
{
    public async Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        if (!context.Headers.TryGetValue("X-API-Key", out var apiKey))
            return AuthenticationResult.Anonymous;

        if (!await validator.IsValid(apiKey))
            return AuthenticationResult.Failed(new AuthenticationFailureReason("Invalid API key"));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "API Client"),
            new Claim(ClaimTypes.AuthenticationMethod, "ApiKey")
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
        return AuthenticationResult.Succeeded(principal);
    }
}
```

### Common Patterns

**Bearer Token:**
```csharp
public class BearerTokenAuthenticationHandler : IAuthenticationHandler
{
    public async Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        if (!context.Headers.TryGetValue("Authorization", out var header))
            return AuthenticationResult.Anonymous;

        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticationResult.Anonymous;

        var token = header["Bearer ".Length..].Trim();
        // validate token, build ClaimsPrincipal...
        return AuthenticationResult.Succeeded(principal);
    }
}
```

**Custom Header:**
```csharp
public class CustomHeaderAuthenticationHandler : IAuthenticationHandler
{
    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        if (!context.Headers.TryGetValue("X-User-ID", out var userId))
            return Task.FromResult(AuthenticationResult.Anonymous);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomHeader"));
        return Task.FromResult(AuthenticationResult.Succeeded(principal));
    }
}
```

### Best Practices

- **Return `Anonymous` when your handler doesn't apply** — never `Failed` just because your header is missing
- **Provide clear failure reasons** — `AuthenticationFailureReason("API key is expired")`
- **Dependency injection works** — handlers can take constructor dependencies
- **Handle exceptions gracefully** — catch validation errors and return `Failed` with a reason

## Microsoft Identity Platform

For Azure-hosted apps using Azure AD / Entra ID, Arc provides built-in support:

```csharp
builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();
```

This registers an ASP.NET Core `AuthenticationHandler` that reads Azure-provided headers:

| Header | Description |
|--------|-------------|
| `x-ms-client-principal` | Base64-encoded Microsoft Client Principal token |
| `x-ms-client-principal-id` | User's unique ID from Azure AD |
| `x-ms-client-principal-name` | User's display name |

These headers are automatically set by Azure Container Apps, Web Apps, and Static Web Apps. You also need:

```csharp
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapIdentityProvider();
```

## Combining Multiple Handlers

Multiple handlers coexist naturally because `Anonymous` means "I don't handle this request":

1. **ApiKeyAuthenticationHandler** — if `X-API-Key` present, authenticates or rejects. If absent, returns `Anonymous`.
2. **MicrosoftIdentityPlatformHandler** — checks for `x-ms-client-principal`. If absent, returns `Anonymous`.
3. If all return `Anonymous` → request is unauthenticated.
