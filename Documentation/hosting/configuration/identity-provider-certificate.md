---
title: "Migrate Cratis Chronicle identity provider certificates"
description: "Replace Chronicle's ignored identityProvider.certificate setting with top-level TLS configuration for the internal OAuth token endpoint."
---

## Migrate to the server TLS certificate

To configure HTTPS for Chronicle's internal OAuth token endpoint, use the top-level [TLS certificate](tls.md), not `identityProvider.certificate`. The legacy setting is accepted for compatibility but ignored, and Chronicle logs a startup warning whenever it is present, even when its `enabled` property is false.

The internal OAuth authority serves `/connect/token` on the shared Chronicle listener. TLS selects the certificate before HTTP path routing, so that path cannot use a different certificate on the same listener.

If your configuration contains `identityProvider.certificate`, migrate it as follows:

1. Keep `tls.enabled=true` and configure the certificate that serves `/connect/token` under top-level `tls.certificatePath` and `tls.certificatePassword`. If an HTTPS reverse proxy fronts Chronicle, keep TLS on the backend for the internal authority.
2. Check that clients reach `/connect/token` over HTTPS and see the expected top-level certificate on the Chronicle listener.
3. Remove the entire `identityProvider.certificate` section and any `Cratis__Chronicle__IdentityProvider__Certificate__*` environment variables.

Turning TLS off is a different topology, not a replacement certificate configuration. With `tls.enabled=false`, authenticated h2c (unencrypted HTTP/2) requires an external HTTPS `authentication.authority`. That authority issues tokens at its own endpoint; Chronicle does not serve `/connect/token` in this topology.

For .NET SDK ClientCredentials authentication against an external authority, configure the client's `Authentication.Authority` for discovery or `Authentication.TokenEndpoint` for an explicit token endpoint. `TokenEndpoint` takes precedence, and both require absolute HTTPS URLs. Callers can also supply bearer tokens themselves, while Workbench browser sessions use cookie login. See [Authentication](authentication.md) when choosing this setup.

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

After restarting Chronicle, confirm that the legacy-setting warning is gone and that `/connect/token` presents the expected certificate when reached directly over HTTPS on the Chronicle listener. If the warning remains, check both `chronicle.json` and the container or host environment for the old section.

The internal OAuth token signing and encryption certificates are separate from the listener's TLS certificate. Continue with [Data Protection Key Encryption](../encryption-certificate.md) to configure the certificate protecting stored key material. An external authority uses its own endpoint and certificate; see [Authentication](authentication.md).
