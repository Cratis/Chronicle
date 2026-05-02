# Local Development & Testing

This reference covers simulating authentication and identity in local development environments without real Azure or identity infrastructure.

## How Identity Works in Development

When running locally:

1. No Azure App Service or ingress injects identity headers
2. The `.cratis-identity` cookie will not be set automatically
3. The frontend cookie reader falls back to calling `GET /.cratis/me`
4. The backend returns a default anonymous identity with empty details

## Generating a Microsoft Client Principal

Azure App Service Easy Auth injects identity via the `X-MS-CLIENT-PRINCIPAL` header. You can simulate this locally with a browser extension.

### Step 1: Build the Principal JSON

```json
{
    "auth_typ": "aad",
    "claims": [
        { "typ": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "val": "user-unique-id" },
        { "typ": "name", "val": "Jane Developer" },
        { "typ": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "val": "Admin" },
        { "typ": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "val": "Manager" }
    ],
    "name_typ": "name",
    "role_typ": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
}
```

### Step 2: Base64-Encode It

**macOS / Linux terminal:**

```bash
echo -n '{"auth_typ":"aad","claims":[{"typ":"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier","val":"user-unique-id"},{"typ":"name","val":"Jane Developer"},{"typ":"http://schemas.microsoft.com/ws/2008/06/identity/claims/role","val":"Admin"}],"name_typ":"name","role_typ":"http://schemas.microsoft.com/ws/2008/06/identity/claims/role"}' | base64
```

**Browser console:**

```javascript
btoa(JSON.stringify({
    auth_typ: "aad",
    claims: [
        { typ: "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", val: "user-unique-id" },
        { typ: "name", val: "Jane Developer" },
        { typ: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", val: "Admin" }
    ],
    name_typ: "name",
    role_typ: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
}));
```

### Step 3: Inject the Header

Use the [ModHeader](https://modheader.com/) browser extension:

1. Install ModHeader for Chrome/Edge/Firefox
2. Add a request header:
   - **Name**: `X-MS-CLIENT-PRINCIPAL`
   - **Value**: the base64 string from Step 2
3. Reload the page — the backend will authenticate the user as if Azure App Service injected the header

## Custom Identity Details in Development

If your `IProvideIdentityDetails<TDetails>` implementation enriches identity from a database, that still runs in development. The principal provides the initial `id`, `name`, and `roles`; your enrichment adds extra fields.

To test without a database, create a dev-only fallback in your identity provider:

```csharp
public Task<UserDetails> Provide(IdentityProviderContext context)
{
    // In development, if the user ID is unknown, return sensible defaults
    var details = await _readModelStore.GetOrDefault<UserDetails>(context.Id, new UserDetails
    {
        Department = "Engineering",
        Title = "Developer"
    });

    return details;
}
```

## Testing Without ModHeader

If you prefer not to install a browser extension, you can also set the cookie directly:

1. Build the identity JSON matching the `.cratis-identity` cookie format
2. Set it via browser console:

```javascript
document.cookie = '.cratis-identity=' + btoa(JSON.stringify({
    id: 'user-id',
    name: 'Jane Developer',
    roles: ['Admin'],
    details: { department: 'Engineering' }
})) + '; path=/';
```

3. Reload — the frontend will use this cookie directly without calling the backend

## Important Notes

- The cookie is `HttpOnly=false` by design — the frontend JavaScript must read it
- The cookie path is `/`
- In production, the cookie is set by the backend middleware; in development, you can set it manually
- The `GET /.cratis/me` endpoint always works — it is the fallback for all environments
- ModHeader header injection simulates the full pipeline (authentication → identity provider → cookie), while direct cookie setting bypasses the backend entirely
