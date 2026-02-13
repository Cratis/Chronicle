# Authentication

Authentication is always enabled. When `authority` is not configured, Chronicle uses its built-in OpenIdDict OAuth authority. When `authority` is set to an external OAuth provider URL, Chronicle will use that instead of the internal authority.

## Example configuration

```json
{
  "authentication": {
    "authority": null,
    "defaultAdminUsername": "admin",
    "defaultAdminPassword": "admin"
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| authority | string | null | External OAuth authority URL |
| defaultAdminUsername | string | "admin" | Default admin username created on first startup |
| defaultAdminPassword | string | "admin" | Default admin password (change in production) |

