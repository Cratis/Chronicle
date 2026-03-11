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
| defaultAdminUsername | string | "admin" | Default admin username created on first startup |

## Development builds

The development image (compiled with the `DEVELOPMENT` preprocessor symbol) supports an additional configuration option for pre-configuring the admin password. This allows development and testing workflows to skip the initial password setup flow.

> **Warning:** This option is only available in development builds (compiled with `DEVELOPMENT` preprocessor symbol) and is removed from production builds at compile time. The password is read from configuration in plain text — use only in isolated development environments. It must never be used in staging or production.

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
| defaultAdminPassword | string | "" (empty) | Pre-configured admin password. When set, the admin user is created with this password and the initial password setup flow is skipped. Only available in development builds. |

When `defaultAdminPassword` is set, the admin user is created with the password already hashed and stored, and `requiresPasswordChange` is set to `false`. If the password is not set, the admin user is created without a password and must go through the initial password setup flow.

