# Authentication

Authentication is always enabled. When `authority` is not configured, Chronicle uses its built-in OpenIdDict OAuth authority. When `authority` is set to an external OAuth provider URL, Chronicle will use that instead of the internal authority.

## Example configuration

```json
{
  "authentication": {
    "authority": null,
    "defaultAdminUsername": "admin"
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| authority | string | null | External OAuth authority URL |
| defaultAdminUsername | string | "admin" | Default admin username created on first startup when `adminUser` is not configured |

## Admin user bootstrap

Chronicle supports pre-configuring the initial admin user's credentials at startup via configuration or secrets management. This is useful for automated or container-based deployments where going through the Workbench's interactive password setup flow is not practical.

### How it works

1. On startup, Chronicle checks whether an admin user with the configured username already exists
2. If no matching user exists and a password is configured, Chronicle:
   - Creates the admin user
   - Hashes the password immediately using ASP.NET Core's `PasswordHasher`
   - Appends the hashed password — the plaintext is **never retained** in memory beyond this point
3. If a user with the same username already exists, the bootstrap step is skipped entirely
4. If no `adminUser` configuration is present (or no password is set), Chronicle falls back to the default behavior: the admin user is created without a password and must go through the initial password setup flow in the Workbench

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

This is the recommended setting for production deployments. It ensures the bootstrap password (which may be stored in secrets management or deployment configuration) is replaced with a password known only to the admin.

## Security considerations

> **Warning:** The `password` value is provided as plaintext in configuration. Always use a proper secrets management solution in production rather than storing the password directly in `chronicle.json`.

**Key security properties of admin user bootstrap:**
- The plaintext password is hashed using ASP.NET Core's `PasswordHasher` immediately on use
- The plaintext value is never persisted to storage, event logs, or application state
- If the admin user already exists when Chronicle restarts, the bootstrap section is completely ignored — credentials are never updated through this mechanism
- Once the admin user has been created, remove or clear the `password` field from your configuration to avoid keeping the bootstrap password in plaintext

**Recommended production workflow:**
1. Set a strong, random initial password via your secrets manager (e.g., Azure Key Vault, Kubernetes Secrets)
2. Set `requirePasswordChangeOnFirstLogin: true`
3. After the first login and password change, clear the `password` from configuration
4. Rotate secrets manager values so the bootstrap password is no longer available

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
