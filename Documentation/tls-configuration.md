# TLS Configuration

Chronicle supports TLS encryption for secure communication between the .NET client and the Kernel server.

## Overview

TLS is enabled by default for both the server and client. The system uses a certificate priority mechanism to determine which certificate to use:

1. **ChronicleOptions** - Certificate path specified in configuration
2. **Embedded Certificate** - Certificate bundled with the application
3. **Dev Certificate** - ASP.NET Core development certificate (server only)

## Server Configuration

### ChronicleOptions Properties

The server-side `ChronicleOptions` (in `Cratis.Chronicle.Configuration` namespace) supports the following TLS properties:

```csharp
public class ChronicleOptions
{
    // Path to the certificate file (e.g., .pfx)
    public string? CertificatePath { get; init; }

    // Password for the certificate file
    public string? CertificatePassword { get; init; }

    // Disable TLS (default: false, TLS enabled)
    public bool DisableTls { get; init; }
}
```

### Configuration via appsettings.json

```json
{
  "Cratis": {
    "Chronicle": {
      "CertificatePath": "/path/to/certificate.pfx",
      "CertificatePassword": "your-password",
      "DisableTls": false
    }
  }
}
```

### Configuration via chronicle.json

```json
{
  "CertificatePath": "/path/to/certificate.pfx",
  "CertificatePassword": "your-password",
  "DisableTls": false
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
    Url = new ChronicleUrl("localhost:35000"),
    CertificatePath = "/path/to/certificate.pfx",
    CertificatePassword = "your-password",
    DisableTls = false  // Default is false (TLS enabled)
};

var client = new ChronicleClient(options);
```

## Certificate Priority

### Server

1. **ChronicleOptions.CertificatePath** - If specified and the file exists
2. **Embedded Certificate** - If `Certs/shared-dev.pfx` exists and is not the placeholder
3. **Dev Certificate** - ASP.NET Core automatically uses development certificates via `UseHttps()`

### Client

1. **ChronicleOptions.CertificatePath** - If specified and the file exists
2. **Embedded Certificate** - If `Certs/shared-dev.pfx` exists and is not the placeholder
3. **No Certificate** - Trusts development certificates and accepts localhost certificate name mismatches

## Embedded Certificates

### For Development

By default, both the server and client include a placeholder certificate file at `Certs/shared-dev.pfx` containing the text "NOT-A-CERTIFICATE". This allows the applications to fall back to development certificates.

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
    Url = new ChronicleUrl("localhost:35000"),
    DisableTls = true
};
```

## Certificate Validation

The client automatically:
- Accepts valid certificates with no errors
- Accepts self-signed certificates that match the client certificate
- Accepts localhost certificates with name mismatches (for development)

This makes it easy to use development certificates while still providing security in production with proper certificates.

## GitHub Actions Integration

The GitHub workflows automatically inject certificates from secrets during the build process:

### publish.yml
- Injects certificates for x64 and arm64 server builds
- Injects certificates for NuGet package builds (both server and client)

### pull-requests.yml
- Injects certificates for PR builds
- Injects certificates for NuGet package builds

The injection uses the `CHRONICLE_CERT_BASE64` secret for the certificate content.
