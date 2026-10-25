---
description: Configure Chronicle Server authentication, using its built-in OpenIdDict authority or an external OAuth provider, behind a proxy or turned off.
---
# Authentication

Authentication is enabled by default. When `authority` is not configured, Chronicle uses its built-in OpenIdDict OAuth authority. When `authority` is set to an external OAuth provider URL, Chronicle will use that instead of the internal authority.

The legacy `identityProvider.certificate` option is ignored; see [Identity Provider Certificate](identity-provider-certificate.md) for migration.

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

## Authentication behind an HTTPS reverse proxy

With `tls.enabled=false`, the Chronicle backend listens using cleartext HTTP/2 only. Authentication in this topology requires an explicit HTTPS **external** `authentication.authority`; startup fails if it is missing or not HTTPS. The internal OpenIddict authority cannot safely derive a public issuer from a cleartext backend, so Chronicle does **not** serve `/connect/token` in h2c mode. OpenIddict never waives its HTTPS requirement based on certificate settings.

Authenticated h2c currently works for callers that obtain a bearer token from the external authority themselves and attach it to HTTP or gRPC requests, and for Workbench browser sessions using Chronicle's cookie login. The .NET SDK's built-in ClientCredentials mode cannot use an external authority: it always requests a token from Chronicle's `/connect/token`, which is absent in this topology. There is no SDK token-endpoint setting to change this. Do not use that mode for an authenticated h2c backend until SDK support is added.

The proxy must forward all Chronicle-bound traffic using h2c and overwrite any client-supplied `X-Forwarded-For` and `X-Forwarded-Proto` headers with the actual client IP and public HTTPS scheme. Configure [trusted reverse-proxy addresses or networks](tls.md#trusted-reverse-proxies) so only that proxy can change the scheme seen by HTTPS-sensitive middleware (including antiforgery) and the effective client IP. Restrict direct connections to the backend. Do not expose the h2c port publicly.

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

### Administrator privileges and recovery

Administrator privilege is stored separately from user documents. Creating or logging in as a user named `admin` does not confer it. New bootstrap grants privilege to the deterministic initial administrator. User replay and stale user-document updates do not overwrite privileges.

On upgrade, Chronicle runs a one-time provenance migration. It reads the lowest-sequence `InitialAdminUserAdded` in the system event log, checks that event's username against the configured administrator username case-insensitively, requires an existing active user for its event source, and rejects provenance if that source has a later `UserRemoved`. This preserves proven legacy administrators with random identifiers. It does not choose a later matching bootstrap event or infer privilege from a username, deterministic identifier, or successful login alone.

The migration records either a proven outcome (granting `UpgradeProvenance` only if no grant history exists), or an unproven outcome with no grant and a warning. An unproven outcome is not retried automatically. Existing grants and revocations remain authoritative across restarts.

To recover an unproven deployment or one without an active administrator:

1. Identify an existing active user. Recovery does not create or reactivate users.
2. Configure `authentication.administratorRecovery` consistently on all silos, using a new nonce for this request:

   ```json
   {
     "authentication": {
       "administratorRecovery": {
         "username": "operator",
         "nonce": "recovery-2026-10-28-unique-request",
         "force": false
       }
     }
   }
   ```

3. Restart the servers. Recovery runs through the roster coordinator, not through a login or HTTP request. By default it refuses to grant when any active administrator already exists. Set `force` to `true` only when deliberately granting despite an existing active administrator.
4. Confirm the `OperatorRecovery` grant in the system event log. Its evidence includes the nonce, executing host, configuration source, and force flag. Recovery also logs a warning.
5. Remove the recovery configuration from every silo. A consumed nonce never grants again, including after revocation; a later recovery requires a new nonce.

Environment variables use the same configuration path:

```shell
Cratis__Chronicle__Authentication__AdministratorRecovery__Username=operator
Cratis__Chronicle__Authentication__AdministratorRecovery__Nonce=recovery-2026-10-28-unique-request
Cratis__Chronicle__Authentication__AdministratorRecovery__Force=false
```

Updated nodes serialize administrator removals and revocations through one guarded roster. Removing the last active administrator fails the command with the error "The last active administrator cannot be removed." Removing another administrator appends its privilege revocation and user removal in one guarded batch. A legacy `UserRemoved` is also interpreted as revocation, but older nodes do not enforce the last-administrator guard: upgrade every writer before relying on it. If an older node removed the only active user, add a user first, then use administrator recovery to grant it the privilege.

This roster does not itself add administrator-only HTTP or gRPC authorization policies. Continue to restrict management access at the deployment boundary; authentication alone is not an administrator permission check.

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

The `@Microsoft.KeyVault(...)` reference is resolved by Azure App Service and Azure Functions when you set it as an application setting — Chronicle does not resolve it. On any other platform the literal text would be used as the value, so inject the resolved secret through that platform's secret mechanism instead, as in the Kubernetes example below.

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
