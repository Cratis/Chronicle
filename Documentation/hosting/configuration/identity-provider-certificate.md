---
title: "Identity Provider Certificate Configuration"
description: "Migrate away from the ignored identityProvider.certificate setting."
---

## Migrate to the server TLS certificate

`identityProvider.certificate` is accepted for compatibility but **ignored**. Chronicle emits a startup warning if the setting is present, even if its `enabled` property is false. It does not choose a separate certificate for the internal OAuth authority. With TLS enabled, the top-level [TLS configuration](tls.md) serves `/connect/token` and every other endpoint on the Chronicle listener. With `tls.enabled=false`, the HTTPS-terminating reverse proxy presents its certificate instead. TLS certificate selection happens before HTTP path routing, so a different certificate cannot be selected for `/connect/token` on the same listener.

If your configuration contains `identityProvider.certificate`:

1. Configure the certificate that should serve `/connect/token` under top-level `tls.certificatePath` and `tls.certificatePassword`, or configure TLS termination at an HTTPS reverse proxy and set `tls.enabled` to `false` for the private backend connection.
2. Check that clients reach `/connect/token` over HTTPS and see the expected certificate from the kernel or reverse proxy.
3. Remove the entire `identityProvider.certificate` section and any `Cratis__Chronicle__IdentityProvider__Certificate__*` environment variables.

For example, replace this ignored setting:

```json
{
  "identityProvider": {
    "certificate": {
      "enabled": true,
      "certificatePath": "/certs/identity-provider.pfx",
      "certificatePassword": "your-password"
    }
  }
}
```

with the certificate for the shared listener:

```json
{
  "tls": {
    "enabled": true,
    "certificatePath": "/certs/server.pfx",
    "certificatePassword": "your-password"
  }
}
```

The internal OAuth token **signing and encryption** certificates are separate from the listener's TLS certificate. See [Data Protection Key Encryption](../encryption-certificate.md) for their configuration. An external authority uses its own endpoint and certificate; see [Authentication](authentication.md).
