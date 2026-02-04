# Data Protection Key Encryption

Chronicle uses ASP.NET Core Data Protection to securely manage encryption keys for OAuth tokens and other sensitive data. In production environments, these keys must be protected with an X.509 certificate to ensure security across multiple Chronicle instances.

## Overview

When Chronicle runs in a clustered environment, all instances need access to the same Data Protection keys to correctly encrypt and decrypt tokens. These keys are stored in MongoDB and shared across all instances using Orleans grains.

In production, an encryption certificate is **required** to protect these keys at rest. In development mode, the certificate is optional for convenience.

## Configuration

### JSON Configuration

Add the encryption certificate configuration to your `chronicle.json`:

```json
{
    "authentication": {
        "encryptionCertificate": {
            "certificatePath": "/path/to/encryption-cert.pfx",
            "certificatePassword": "your-certificate-password"
        }
    }
}
```

### Environment Variables

Configure using environment variables (recommended for containerized deployments):

```bash
# Path to the PFX certificate file
Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePath=/app/certs/encryption-cert.pfx

# Certificate password
Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePassword=your-certificate-password
```

### Configuration Properties

| Property             | Type   | Required (Production) | Description                                |
|----------------------|--------|----------------------|---------------------------------------------|
| certificatePath      | string | Yes                  | Path to the PFX certificate file            |
| certificatePassword  | string | Yes*                 | Password for the certificate (* can be empty if cert has no password) |

## Generating a Certificate

You can use the same certificate for both TLS and Data Protection key encryption, or generate a separate certificate specifically for key encryption.

### Using .NET CLI

```bash
# Generate a certificate for key encryption
dotnet dev-certs https -ep ./encryption-cert.pfx -p YourSecurePassword123
```

### Using OpenSSL

```bash
# Generate a private key
openssl genrsa -out encryption.key 2048

# Create a self-signed certificate
openssl req -x509 -new -nodes -key encryption.key -sha256 -days 3650 \
    -out encryption.crt \
    -subj "/CN=Chronicle Data Protection/O=Your Organization"

# Convert to PFX format
openssl pkcs12 -export -out encryption-cert.pfx \
    -inkey encryption.key -in encryption.crt \
    -password pass:YourSecurePassword123
```

## Docker Configuration

Mount the certificate when running Chronicle in Docker:

```yaml
version: '3.8'

services:
  chronicle:
    image: cratis/chronicle:latest
    ports:
      - "8080:8080"
      - "35000:35000"
    volumes:
      - ./certs/encryption-cert.pfx:/app/certs/encryption-cert.pfx:ro
    environment:
      - Cratis__Chronicle__Storage__ConnectionDetails=mongodb://mongodb:27017
      - Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePath=/app/certs/encryption-cert.pfx
      - Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePassword=YourSecurePassword123
```

### Using Same Certificate for TLS and Encryption

You can use the same PFX certificate for both TLS and Data Protection key encryption:

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest
    volumes:
      - ./certs/chronicle.pfx:/app/certs/chronicle.pfx:ro
    environment:
      # TLS configuration
      - Cratis__Chronicle__Tls__CertificatePath=/app/certs/chronicle.pfx
      - Cratis__Chronicle__Tls__CertificatePassword=YourPassword
      # Data Protection key encryption (same certificate)
      - Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePath=/app/certs/chronicle.pfx
      - Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePassword=YourPassword
```

## Multi-Instance Deployments

In clustered deployments, all Chronicle instances must use the **same** encryption certificate. This ensures that any instance can decrypt Data Protection keys stored in the shared MongoDB database.

```yaml
services:
  chronicle-1:
    image: cratis/chronicle:latest
    environment:
      - Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePath=/app/certs/encryption-cert.pfx
      - Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePassword=SharedPassword
    volumes:
      - ./certs/encryption-cert.pfx:/app/certs/encryption-cert.pfx:ro

  chronicle-2:
    image: cratis/chronicle:latest
    environment:
      - Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePath=/app/certs/encryption-cert.pfx
      - Cratis__Chronicle__Authentication__EncryptionCertificate__CertificatePassword=SharedPassword
    volumes:
      - ./certs/encryption-cert.pfx:/app/certs/encryption-cert.pfx:ro
```

## Development Mode

In development mode (when Chronicle is built with the `DEVELOPMENT` configuration), the encryption certificate is **optional**. This allows for easier local development without certificate management overhead.

To run without a certificate in development:

```bash
# No certificate configuration needed in development
docker run -d \
  --name chronicle-dev \
  -p 8080:8080 \
  -p 35000:35000 \
  -e Cratis__Chronicle__Storage__ConnectionDetails=mongodb://localhost:27017 \
  cratis/chronicle:latest-development
```

## Security Best Practices

1. **Use strong passwords** - Certificate passwords should be complex and unique
2. **Protect certificate files** - Store certificates securely and limit access
3. **Rotate certificates** - Implement a certificate rotation strategy for production
4. **Separate concerns** - Consider using different certificates for TLS and key encryption
5. **Backup certificates** - Ensure certificates are backed up securely; losing the certificate means losing access to encrypted keys
6. **Use secrets management** - In production, use a secrets manager (Azure Key Vault, HashiCorp Vault, etc.) to store certificate passwords

## Troubleshooting

### Certificate Not Found

If you see an error about the certificate not being found:

1. Verify the certificate path is correct and accessible
2. Check file permissions on the certificate file
3. Ensure the path uses forward slashes in Docker/Linux environments

### Invalid Certificate Password

If authentication fails after configuration:

1. Verify the certificate password is correct
2. Check for special characters that may need escaping in environment variables
3. Ensure the password matches what was used during certificate generation

### Production Startup Failure

In production, Chronicle will fail to start if no encryption certificate is configured:

```
InvalidOperationException: An encryption certificate is required in production for Data Protection key security.
Configure 'Authentication:EncryptionCertificate:CertificatePath' and 'Authentication:EncryptionCertificate:CertificatePassword'
in your configuration.
```

Ensure you have configured the certificate path and password as described above.

## Next Steps

- [Local Certificates](hosting/local-certificates.md) - TLS certificate setup for development
- [Production Hosting](hosting/production.md) - Production deployment requirements
- [Configuration](hosting/configuration.md) - Complete configuration reference

