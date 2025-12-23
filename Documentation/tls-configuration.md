# TLS Configuration

Chronicle supports TLS encryption for secure communication between the .NET client and the Kernel server.

## Overview

TLS is enabled by default for both the server and client. The system uses a certificate priority mechanism to determine which certificate to use:

1. **ChronicleOptions** - Certificate path specified in configuration
2. **Embedded Certificate** - Certificate bundled with the application
3. **Development Mode Auto-Generated Certificate** - In-memory CA and server certificate (development builds only)
4. **Dev Certificate** - ASP.NET Core development certificate (server fallback)

## Server Configuration

### ChronicleOptions Properties

You can configure the behavior of the Chronicle Server and TLS through properties in either through the `chronicle.json` file or through environment variables that you mount for the
container.

### The chronicle.json File

```json
{
    "tls": {
        "CertificatePath": "/path/to/certificate.pfx",
        "CertificatePassword": "your-password",
        "Disable": false   // Default is false (TLS enabled)
    }
}
```

### Environment Variables

```bash
Cratis__Chronicle__CertificatePath=/path/to/certificate.pfx
Cratis__Chronicle__CertificatePassword=your-password
Cratis__Chronicle__DisableTls=false
```

## Client Configuration

### ChronicleOptions Properties

The client-side `ChronicleOptions` (in `Cratis.Chronicle` namespace) supports the following TLS properties:

```csharp
var options = new ChronicleOptions
{
    Url = new ChronicleConnectionString("localhost:35000"),
    CertificatePath = "/path/to/certificate.pfx",
    CertificatePassword = "your-password",
    DisableTls = false,  // Default is false (TLS enabled)
    ManagementPort = 8080  // Port to fetch development CA from server (default: 8080)
};

var client = new ChronicleClient(options);
```

## Certificate Priority

### Server

1. **ChronicleOptions.CertificatePath** - If specified and the file exists
2. **Embedded Certificate** - If `Certs/shared-dev.pfx` exists and is not the placeholder
3. **Development Mode Auto-Generated Certificate** - In Debug builds with `DEVELOPMENT` symbol, an ephemeral CA and server certificate are generated at startup
4. **Dev Certificate** - ASP.NET Core automatically uses development certificates via `UseHttps()`

### Client

1. **ChronicleOptions.CertificatePath** - If specified and the file exists
2. **Embedded Certificate** - If `Certs/shared-dev.pfx` exists and is not the placeholder
3. **Development CA Fetch** - When no certificate is configured, the client fetches the development CA from `http://<server>:<ManagementPort>/.well-known/chronicle/ca` and uses it for TLS validation
4. **No Certificate** - Trusts development certificates and accepts localhost certificate name mismatches

## Embedded Certificates

### For Local Development

By default, both the server and client include a placeholder certificate file at `Certs/shared-dev.pfx` containing the text "NOT-A-CERTIFICATE". This allows the applications to fall back to development certificates.

**In development builds** (Development Docker Image):

- The server automatically generates an ephemeral Certificate Authority (CA) and server certificate at startup
- The CA certificate is exposed via HTTP at `http://<server>:<ManagementPort>/.well-known/chronicle/ca` (default port: 8080)
- Clients without a configured certificate automatically fetch and trust this development CA
- No certificate files need to be created or distributed for local development
- The generated certificates are kept in-memory only and regenerated on each server restart

This approach provides:

- **Zero-config TLS** for local development
- **No private keys** stored in the repository or Docker images
- **Automatic trust** between development server and clients
- **Secure by default** - TLS is always enabled even in development

### For Production

During the build and publish process, you can replace the placeholder certificate with a real certificate:

1. Create a real `.pfx` certificate file
2. Encode it as base64: `base64 certificate.pfx > certificate.txt`
3. Add the base64-encoded certificate as a GitHub secret named `CHRONICLE_CERT_BASE64`
4. Add the certificate password as a GitHub secret named `CHRONICLE_CERT_PASSWORD`
5. The GitHub workflows will automatically inject the certificate during build

## Disabling TLS

To disable TLS (e.g., for local development or testing):

### Server

```json
{
  "Cratis": {
    "Chronicle": {
      "DisableTls": true
    }
  }
}
```

### Client

```csharp
var options = new ChronicleOptions
{
    Url = new ChronicleConnectionString("localhost:35000"),
    DisableTls = true
};
```

## Certificate Validation

The client automatically:
- Accepts valid certificates with no errors
- Accepts self-signed certificates that match the client certificate
- **In development mode**: Fetches the development CA from the server and uses it to validate the TLS connection using a custom trust store
- Accepts localhost certificates with name mismatches (for development)

This makes it easy to use development certificates while still providing security in production with proper certificates.

## Development Mode

### How It Works

When running in Debug configuration (with the `DEVELOPMENT` symbol defined):

1. **Server startup**:
   - Generates an ephemeral RSA key pair and creates a self-signed Certificate Authority (CA)
   - Creates a server certificate signed by the CA (subject: `CN=localhost`)
   - Configures Kestrel to use the generated server certificate for HTTPS
   - Exposes the CA certificate (PEM format) at `/.well-known/chronicle/ca` on the configured `ManagementPort` (default: 8080)
   - Logs: `Generated development certificate and CA for local development`

2. **Client connection**:
   - If no certificate is configured, attempts to fetch the development CA from `http://<server>:<ManagementPort>/.well-known/chronicle/ca`
   - If successful, uses the fetched CA in a custom trust store (via `X509ChainTrustMode.CustomRootTrust`)
   - Validates the server's TLS certificate against the fetched CA during the TLS handshake
   - Logs: `Fetching development CA from <url>`, `Fetched development CA from <url>`, `Using fetched development CA for validation`

3. **TLS handshake**:
   - The client builds an X509 chain using the fetched CA as a trusted root
   - The server's certificate is validated against this chain
   - Connection succeeds with full TLS encryption

### Benefits

- **No manual certificate setup** required for local development
- **No private keys** in source control or Docker images
- **Automatic trust** between server and client in development
- **Secure by default** - TLS always enabled
- **Simple Docker development** - the development image includes the auto-generated certificate logic

### Disabling Development Mode

In production builds (Release configuration), the `DEVELOPMENT` symbol is not defined, so:
- The server uses the configured certificate path, embedded certificate, or ASP.NET Core dev certificate
- The client does not attempt to fetch a development CA
- Standard certificate validation applies

## GitHub Actions Integration

The GitHub workflows automatically inject certificates from secrets during the build process:

### publish.yml
- Injects certificates for x64 and arm64 server builds
- Injects certificates for NuGet package builds (both server and client)

### pull-requests.yml
- Injects certificates for PR builds
- Injects certificates for NuGet package builds

The injection uses the `CHRONICLE_CERT_BASE64` secret for the certificate content.
