---
title: "Identity Provider Certificate Configuration"
description: "Migrate away from the ignored identityProvider.certificate setting."
---

## Migrate to the server TLS certificate

`identityProvider.certificate` is accepted for compatibility but **ignored**. Chronicle emits a startup warning if the setting is present, even if its `enabled` property is false. The internal OAuth authority serves `/connect/token` using the top-level [TLS certificate](tls.md), not a separate certificate. TLS certificate selection happens before HTTP path routing, so a different certificate cannot be selected for that path on the same listener.

If your configuration contains `identityProvider.certificate`:

1. Keep `tls.enabled=true` and configure the certificate that serves `/connect/token` under top-level `tls.certificatePath` and `tls.certificatePassword`. If an HTTPS reverse proxy fronts Chronicle, keep TLS on the backend for the internal authority.
2. Check that clients reach `/connect/token` over HTTPS and see the expected top-level certificate on the Chronicle listener.
3. Remove the entire `identityProvider.certificate` section and any `Cratis__Chronicle__IdentityProvider__Certificate__*` environment variables.

`tls.enabled=false` is **not** a replacement for the identity provider certificate: authenticated h2c requires an external HTTPS `authentication.authority`, which issues tokens at its own endpoint. Chronicle no longer serves `/connect/token` in that topology. Migrating to an external authority also requires changing clients' token configuration; see [Authentication](authentication.md).

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
