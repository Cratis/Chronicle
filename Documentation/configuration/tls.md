# TLS Configuration (Client)

Chronicle .NET clients support TLS for secure communication with Chronicle Server. TLS is enabled by default on the server, but can be disabled.

For server-side TLS configuration, see [TLS Configuration (Server)](../hosting/configuration/tls.md).

## appsettings.json

```json
{
  "Chronicle": {
    "Url": "chronicle://localhost:35000",
    "Tls": {
      "CertificatePath": "/path/to/certificate.pfx",
      "CertificatePassword": "your-password",
      "Disable": false
    }
  }
}
```

## Client options

```csharp
var options = new ChronicleOptions
{
    ConnectionString = "chronicle://localhost:35000",
    Tls = new Tls
    {
        CertificatePath = "/path/to/certificate.pfx",
        CertificatePassword = "your-password",
        Disable = false
    }
};

var client = new ChronicleClient(options);
```

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| CertificatePath | string | null | Path to the client certificate (PFX format) if mutual TLS is used |
| CertificatePassword | string | null | Password for the certificate file |
| Disable | boolean | false | Disable TLS for development servers running without TLS |

## Connection string option

TLS can also be disabled through the connection string in development:

```csharp
var options = ChronicleOptions.FromConnectionString("chronicle://localhost:35000?disableTls=true");
```

## Development vs production

When we talk about **Development** vs **Production**, we're talking about the development Docker image
vs the Production Docker image.

- **Development**: TLS is disabled by default.
- **Production**: TLS should remain enabled but can be disabled.

## Certificate validation

The client validates server certificates using standard TLS rules:

- Valid certificates are accepted without extra configuration.
- Self-signed certificates require trust on the client machine.
- Name mismatches for localhost are accepted for development.

## Troubleshooting

### Client connection errors

**Error**: "The remote certificate is invalid"

**Solutions**:
1. Ensure the server certificate is valid and not expired.
2. Verify the client trusts the certificate authority.
3. For development, install the self-signed certificate in the system trust store.
4. As a last resort for development only, set `Disable` to true.


