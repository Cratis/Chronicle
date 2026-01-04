# TLS Configuration

Chronicle supports TLS encryption for secure communication between the .NET client and the Kernel server.

## Overview

TLS can be enabled or disabled based on your environment needs:

- **Development**: TLS is disabled by default for easier local development
- **Production**: TLS should be enabled with properly configured certificates

## Server Configuration

### Configuration File (chronicle.json)

```json
{
    "tls": {
        "certificatePath": "/path/to/certificate.pfx",
        "certificatePassword": "your-password",
        "disable": false   // Set to true to disable TLS
    }
}
```

### Environment Variables

```bash
Cratis__Chronicle__Tls__CertificatePath=/path/to/certificate.pfx
Cratis__Chronicle__Tls__CertificatePassword=your-password
Cratis__Chronicle__Tls__Disable=false
```

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CertificatePath` | string | null | Path to the TLS certificate file (PFX format) |
| `CertificatePassword` | string | null | Password for the certificate file |
| `Disable` | bool | false | Whether to disable TLS |

## Client Configuration

The client-side `ChronicleOptions` supports the following TLS properties:

```csharp
var options = new ChronicleOptions
{
    ConnectionString = "chronicle://localhost:35000",
    Tls = new Tls
    {
        CertificatePath = "/path/to/certificate.pfx",
        CertificatePassword = "your-password",
        Disable = false  // Set to true to disable TLS
    }
};

var client = new ChronicleClient(options);
```

## Development vs Production

### Development

For local development, TLS is disabled by default in the `chronicle.json` file:

```json
{
    "tls": {
        "disable": true
    }
}
```

This allows for easier local development without certificate management. However, you can enable TLS for development to test TLS-related functionality. See [Local Certificate Setup](local-certificates.md) for guidance on generating and configuring development certificates.

### Production

In production environments, TLS should be enabled and properly configured:

```json
{
    "tls": {
        "certificatePath": "/app/certs/production.pfx",
        "certificatePassword": "${CERT_PASSWORD}",
        "disable": false
    }
}
```

**Important**: If TLS is enabled (`disable: false`) but no certificate is provided, the server will fail to start with an error message:

```
TLS is enabled but no certificate is configured. Please provide a certificate path in configuration or disable TLS.
```

## Certificate Requirements

Chronicle requires certificates in **PFX (PKCS#12)** format. The certificate should include:

- A valid private key
- The certificate chain (if applicable)

### Obtaining Certificates

For production environments, obtain certificates from:

- A trusted Certificate Authority (CA) like Let's Encrypt, DigiCert, etc.
- Your organization's internal CA
- Cloud provider certificate services (AWS Certificate Manager, Azure Key Vault, etc.)

For development environments, generate self-signed certificates:

- Using the [.NET CLI](local-certificates.md#option-1-using-net-cli-recommended-for-net-developers)
- Using [OpenSSL](local-certificates.md#option-2-using-openssl)

## Connection String Configuration

The client can also disable TLS through the connection string:

```csharp
var options = ChronicleOptions.FromConnectionString("chronicle://localhost:35000?disableTls=true");
```

## Certificate Validation

When TLS is enabled:

### Server Validation

The server uses the configured certificate for all HTTPS endpoints:
- Management API (default port 8080)
- gRPC service (default port 35000)

### Client Validation

The client validates server certificates using standard TLS validation rules:

- **Valid certificates**: Accepted without additional configuration
- **Self-signed certificates**: Requires certificate trust configuration on the client machine
- **Certificate name mismatches**: For development with localhost, name mismatches are accepted

## Docker Deployment

When running Chronicle in Docker with TLS enabled, mount the certificate file:

```yaml
version: '3.8'

services:
  chronicle:
    image: cratis/chronicle:latest
    ports:
      - "8080:8080"
      - "35000:35000"
    volumes:
      - ./chronicle.json:/app/chronicle.json:ro
      - ./certs/production.pfx:/app/certs/production.pfx:ro
    environment:
      - Cratis__Chronicle__Tls__CertificatePath=/app/certs/production.pfx
      - Cratis__Chronicle__Tls__CertificatePassword=${CERT_PASSWORD}
```

## Security Best Practices

1. **Use TLS in Production**: Always enable TLS for production deployments
2. **Protect Certificate Passwords**: Use environment variables or secrets management
3. **Certificate Rotation**: Implement a process for regular certificate renewal
4. **Strong Encryption**: Use modern TLS protocols (TLS 1.2 or higher)
5. **Secure Storage**: Protect certificate files with appropriate file permissions
6. **Trusted CAs**: Use certificates from trusted Certificate Authorities in production

## Troubleshooting

### Server Fails to Start

**Error**: "TLS is enabled but no certificate is configured"

**Solution**: Either provide a certificate path in the configuration or disable TLS:
```json
{
    "tls": {
        "disable": true
    }
}
```

### Client Connection Errors

**Error**: "The remote certificate is invalid"

**Solutions**:
1. Ensure the server certificate is valid and not expired
2. Verify the client trusts the certificate authority
3. For development, install the self-signed certificate in the system trust store
4. As a last resort for development only, disable TLS validation (not recommended)

### Certificate Not Found

**Error**: Certificate file not found at the specified path

**Solutions**:
1. Verify the file path is correct and accessible
2. Use absolute paths to avoid working directory issues
3. Check file permissions ensure the application can read the certificate
4. In Docker, verify the volume mount is correctly configured

## See Also

- [Local Certificate Setup](local-certificates.md) - Generate and configure development certificates
- [Production Hosting](production.md) - Production deployment guidance
- [Configuration](configuration.md) - Complete configuration reference

