---
title: "Identity Provider Certificate Configuration"
description: "The identityProvider.certificate setting, and why Chronicle does not apply it as of 19.6."
---

:::caution[Not applied as of Chronicle 19.6]
The kernel accepts the `identityProvider.certificate` settings described below, but does not use them: the identity provider endpoints are served on port 35000 with the top-level [TLS certificate](tls.md), like everything else. Configure the TLS certificate; a dedicated identity provider certificate currently has no effect.
:::

When Chronicle uses the internal OAuth authority (`authentication.authority` is not set), you can configure a dedicated certificate for identity provider endpoints.

This certificate configuration is separate from the top-level TLS certificate and uses its own configuration path:

- `identityProvider.certificate`

## Fallback behavior

Identity provider certificate resolution follows this order:

1. If `identityProvider.certificate` is set, use it.
2. If `identityProvider.certificate` is not set, fall back to top-level `tls`.

This preserves backward compatibility with existing configurations that only use `tls`.

## Configuration

### Dedicated identity provider certificate

```json
{
  "authentication": {
    "authority": null
  },
  "identityProvider": {
    "certificate": {
      "enabled": true,
      "certificatePath": "/certs/identity-provider.pfx",
      "certificatePassword": "your-password"
    }
  }
}
```

### Reuse top-level TLS certificate (fallback)

```json
{
  "tls": {
    "enabled": true,
    "certificatePath": "/certs/server.pfx",
    "certificatePassword": "your-password"
  },
  "authentication": {
    "authority": null
  }
}
```

In this configuration, `identityProvider.certificate` is not set, so Chronicle uses `tls` for identity provider endpoint scheme decisions.

## Environment variables

```bash
Cratis__Chronicle__IdentityProvider__Certificate__Enabled=true
Cratis__Chronicle__IdentityProvider__Certificate__CertificatePath=/certs/identity-provider.pfx
Cratis__Chronicle__IdentityProvider__Certificate__CertificatePassword=your-password
```

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| identityProvider.certificate.enabled | boolean | true | Whether TLS is enabled for identity provider endpoints |
| identityProvider.certificate.certificatePath | string | null | Path to the identity provider certificate file (PFX format) |
| identityProvider.certificate.certificatePassword | string | null | Password for the identity provider certificate file |

## Related topics

- [Authentication](authentication.md)
- [TLS](tls.md)
