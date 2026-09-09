# Authentication

Authentication is enabled by default. When `authority` is not configured, Chronicle uses its built-in OpenIdDict OAuth authority. When `authority` is set to an external OAuth provider URL, Chronicle will use that instead of the internal authority.

Identity provider certificate configuration is documented on [Identity Provider Certificate](identity-provider-certificate.md).

## Example configuration

```json
{
  "authentication": {
    "enabled": true,
    "authority": null,
    "defaultAdminUsername": "admin"
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| enabled | bool | true | Whether authentication is enforced. See [Turning authentication off](#turning-authentication-off) |
| authority | string | null | External OAuth authority URL |
| audience | string | "chronicle" | Required audience for external-authority access tokens |
| requireHttpsMetadata | bool | true | Require HTTPS when retrieving external-authority metadata |
| defaultAdminUsername | string | "admin" | Default admin username created on first startup when `adminUser` is not configured |

## Turning authentication off

Setting `enabled` to `false` removes authentication from the server entirely. There is no token authority, no
`/connect/token` endpoint, no identity endpoints and no bootstrap admin user, and every gRPC service and HTTP
endpoint answers anonymously. A client connects to it with `auth=none` in its connection string:

```text
chronicle://localhost:35000?auth=none
```

**This is only for a Chronicle that is not reachable as a server.** It exists for an instance embedded in a
single container or process, talking to its own client over loopback, thrown away with the process — a play
sandbox, a disposable test host. There the credential exchange protects nothing and costs every client
seconds of cold start: acquiring the first access token takes 1.9 seconds on an unconstrained machine and 3.7
under a half-core container limit, almost all of it warming the token endpoint's request pipeline.

Anywhere a network can reach the server, leaving this off publishes the whole event store — every event, every
read model, every management operation — to anyone who can open a socket to it. It is not a development
convenience to be left on by accident; treat it as part of the deployment topology.

A client using `auth=none` against a server that does enforce authentication fails every call as
unauthenticated. Note also that a connection string with *no* credentials does not mean this — it still
performs a client-credentials exchange using the development credentials, which is what it has always done.
`auth=none` is the explicit form.

## Admin user bootstrap

Chronicle supports pre-configuring the initial admin user's credentials at startup via configuration or secrets management. This is useful for automated or container-based deployments where going through the Workbench's interactive password setup flow is not practical.

### How it works

1. `adminUser.username` takes precedence over `defaultAdminUsername`; an empty or whitespace-only override uses the default. Existing users are matched case-insensitively, preserving their deployed identifiers.
2. A new administrator uses one deterministic identifier across silos. Creation and initial password events use event-source and event-type concurrency checks: concurrent startup cannot append the same bootstrap step twice.
3. A configured password initializes only an administrator without existing credentials. A restart can resume incomplete initialization, but never overwrites a password that has already been set.
4. Without a configured password, the Workbench offers setup for the configured administrator only. Confirmation must match. Once credentials exist, including temporary credentials from older deployments, anonymous setup is rejected.
5. Creation and password operations wait for the persisted user state. Append failures or a projection that does not complete are reported instead of returning successful setup. A completion timeout does not undo an accepted event; inspect the user state before retrying.

Keep administrator configuration consistent across silos. Changing the bootstrap username is not an account-rename mechanism. Restrict network access until interactive initial setup has completed, or supply the initial password through your secret mechanism.

### Configuration file

```json
{
  "authentication": {
    "adminUser": {
      "username": "admin",
      "password": "a-strong-initial-password",
      "email": "admin@example.com",
      "requirePasswordChangeOnFirstLogin": true
    }
  }
}
```

### Environment variables

```bash
Cratis__Chronicle__Authentication__AdminUser__Username=admin
Cratis__Chronicle__Authentication__AdminUser__Password=a-strong-initial-password
Cratis__Chronicle__Authentication__AdminUser__Email=admin@example.com
Cratis__Chronicle__Authentication__AdminUser__RequirePasswordChangeOnFirstLogin=true
```

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| username | string | "" (uses `defaultAdminUsername`) | The admin username. Falls back to `defaultAdminUsername` when empty |
| password | string | "" | The admin password in plaintext. Hashed internally on load |
| email | string | "" | The admin user's email address |
| requirePasswordChangeOnFirstLogin | bool | false | When `true`, the admin can log in but must change their password before continuing |

### `requirePasswordChangeOnFirstLogin`

When this option is `true`:
- The admin user is created with the configured password
- On first login, Chronicle redirects the admin to the password change screen
- The admin must set a new password before accessing the Workbench

## Security considerations

The `password` value should be sourced from a secrets management solution such as Azure Key Vault, Kubernetes Secrets, or Docker Secrets rather than stored directly in `chronicle.json`.

**Key security properties of admin user bootstrap:**
- Credentials are hashed before being appended or persisted. Plaintext configuration values can remain in the process configuration; protect that configuration and never log it.
- Bootstrap never replaces existing credentials. It may finish initialization of an existing administrator that still has no password.

### Browser sessions and request protection

The Workbench uses an HTTP-only, Secure, SameSite=Lax authentication cookie. Serve it over HTTPS, including local reverse-proxy deployments. Authenticated cookie requests using state-changing methods require an antiforgery token, even if the endpoint also permits anonymous callers. Only the login and token-exchange endpoints explicitly bypass this validation.

After signing in, obtain a token with `GET /.cratis/antiforgery` using the authentication cookie. Send the returned `requestToken` as `X-CSRF-TOKEN` on state-changing requests, including `POST /identity/logout`. Refresh it after changing the signed-in identity; do not reuse a token from an earlier session. The Workbench manages this lifecycle automatically. With authentication disabled, the endpoint returns `204` and no request token is required.

Only Identity login and refresh are anonymous. Registration and account-management endpoints require authentication. Bearer-only HTTP and gRPC callers do not require an antiforgery token; explicit bearer credentials take precedence over an ambient browser cookie.

### Token compatibility

Chronicle-issued access tokens now target the `chronicle` audience, and validation requires it. Previously issued tokens without that audience must be reacquired after upgrading. External authorities must issue tokens for the configured `audience` and provide a meaningful subject. Missing or blank authenticated subjects are rejected rather than replaced with random or system identities.

These safeguards do not introduce role-based operator policies or enforce a future restricted-session workflow. `requirePasswordChangeOnFirstLogin` drives the Workbench password-change prompt; it is not an authorization boundary preventing other authenticated API calls.

### Azure Key Vault

```bash
# Store the initial password in Key Vault
az keyvault secret set --vault-name my-vault --name chronicle-admin-password --value "strong-random-password"
```

```bash
# Reference in environment variables
Cratis__Chronicle__Authentication__AdminUser__Username=admin
Cratis__Chronicle__Authentication__AdminUser__Password=@Microsoft.KeyVault(SecretUri=https://my-vault.vault.azure.net/secrets/chronicle-admin-password)
Cratis__Chronicle__Authentication__AdminUser__RequirePasswordChangeOnFirstLogin=true
```

### Kubernetes Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: chronicle-admin
type: Opaque
stringData:
  admin-password: "strong-random-password"
---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
        - name: chronicle
          env:
            - name: Cratis__Chronicle__Authentication__AdminUser__Username
              value: "admin"
            - name: Cratis__Chronicle__Authentication__AdminUser__Password
              valueFrom:
                secretKeyRef:
                  name: chronicle-admin
                  key: admin-password
            - name: Cratis__Chronicle__Authentication__AdminUser__RequirePasswordChangeOnFirstLogin
              value: "true"
```

### Docker Compose

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest
    environment:
      - Cratis__Chronicle__Authentication__AdminUser__Username=admin
      - Cratis__Chronicle__Authentication__AdminUser__Password=${ADMIN_PASSWORD}
      - Cratis__Chronicle__Authentication__AdminUser__RequirePasswordChangeOnFirstLogin=true
```

## Development image

The development image (compiled with the `DEVELOPMENT` preprocessor symbol) supports an additional configuration option for pre-configuring the admin password. This is a legacy mechanism — new deployments should use the `adminUser` section above, which works in all environments.

> **Warning:** `defaultAdminPassword` is only available in the development image (compiled with `DEVELOPMENT` preprocessor symbol) and is removed from production builds at compile time. The password is read from configuration in plain text — use only in isolated development environments. It must never be used in staging or production.

```json
{
  "authentication": {
    "defaultAdminUsername": "admin",
    "defaultAdminPassword": "YourDevPassword"
  }
}
```

You can also configure this through an environment variable:

```shell
Cratis__Chronicle__Authentication__DefaultAdminPassword=YourDevPassword
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| defaultAdminPassword | string | "" (empty) | Pre-configured admin password. When set, the admin user is created with this password and the initial password setup flow is skipped. Only available in the development image. |

When `defaultAdminPassword` is set, the admin user is created with the password already hashed and stored, and `requiresPasswordChange` is set to `false`. If the password is not set, the admin user is created without a password and must go through the initial password setup flow.
