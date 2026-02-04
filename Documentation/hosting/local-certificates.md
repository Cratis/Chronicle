# Local Certificate Setup for Development

This guide explains how to set up TLS certificates for local Chronicle development. While TLS is disabled by default for development, you may want to enable it to test TLS-related functionality or to simulate production environments.
The client will connect to the Chronicle Kernel unsecure if no certificate is specified.

## Overview

Chronicle supports TLS certificates in PFX (PKCS#12) format. You can generate certificates using either OpenSSL or the .NET CLI tools. This guide covers both approaches.

## Option 1: Using .NET CLI (Recommended for .NET Developers)

The .NET CLI provides a simple way to generate development certificates that are automatically trusted by your system.

### Generate a Development Certificate

```bash
# Generate a development certificate
dotnet dev-certs https -ep ./chronicle-dev.pfx -p YourPassword123

# Trust the certificate (optional, for client applications)
dotnet dev-certs https --trust
```

### Configure Chronicle to Use the Certificate

Update your `chronicle.json` configuration:

```json
{
  "tls": {
    "certificatePath": "./chronicle-dev.pfx",
    "certificatePassword": "YourPassword123"
  }
}
```

Or use environment variables:

```bash
export Cratis__Chronicle__Tls__CertificatePath=./chronicle-dev.pfx
export Cratis__Chronicle__Tls__CertificatePassword=YourPassword123
```

## Option 2: Using OpenSSL

OpenSSL provides more control over certificate generation and is useful for advanced scenarios.

### Generate a Self-Signed Certificate

```bash
# Generate a private key
openssl genrsa -out chronicle-dev.key 2048

# Create a certificate signing request
openssl req -new -key chronicle-dev.key -out chronicle-dev.csr \
  -subj "/CN=localhost/O=Development/C=US"

# Generate a self-signed certificate
openssl x509 -req -days 365 -in chronicle-dev.csr \
  -signkey chronicle-dev.key -out chronicle-dev.crt

# Convert to PFX format (required by Chronicle)
openssl pkcs12 -export -out chronicle-dev.pfx \
  -inkey chronicle-dev.key -in chronicle-dev.crt \
  -password pass:YourPassword123
```

### Generate a Certificate with Subject Alternative Names (SAN)

For production-like testing with custom domains:

```bash
# Create a configuration file for SAN
cat > san.cnf << EOF
[req]
default_bits = 2048
prompt = no
default_md = sha256
distinguished_name = dn
req_extensions = req_ext

[dn]
CN = localhost
O = Development
C = US

[req_ext]
subjectAltName = @alt_names

[alt_names]
DNS.1 = localhost
DNS.2 = chronicle.local
IP.1 = 127.0.0.1
EOF

# Generate the certificate with SAN
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout chronicle-dev.key -out chronicle-dev.crt \
  -config san.cnf -extensions req_ext

# Convert to PFX
openssl pkcs12 -export -out chronicle-dev.pfx \
  -inkey chronicle-dev.key -in chronicle-dev.crt \
  -password pass:YourPassword123
```

### Configure Chronicle

Update your `chronicle.json`:

```json
{
  "tls": {
    "certificatePath": "./chronicle-dev.pfx",
    "certificatePassword": "YourPassword123"
  }
}
```

## Trusting Self-Signed Certificates

For local development, you may want to trust your self-signed certificate to avoid browser warnings.

### Windows

```powershell
# Import the certificate
Import-PfxCertificate -FilePath chronicle-dev.pfx `
  -CertStoreLocation Cert:\LocalMachine\Root `
  -Password (ConvertTo-SecureString -String "YourPassword123" -AsPlainText -Force)
```

### macOS

```bash
# Extract the certificate from PFX
openssl pkcs12 -in chronicle-dev.pfx -clcerts -nokeys -out chronicle-dev.crt \
  -password pass:YourPassword123

# Add to system keychain
sudo security add-trusted-cert -d -r trustRoot \
  -k /Library/Keychains/System.keychain chronicle-dev.crt
```

### Linux (Ubuntu/Debian)

```bash
# Extract the certificate from PFX
openssl pkcs12 -in chronicle-dev.pfx -clcerts -nokeys -out chronicle-dev.crt \
  -password pass:YourPassword123

# Copy to the trusted certificates directory
sudo cp chronicle-dev.crt /usr/local/share/ca-certificates/

# Update the certificate store
sudo update-ca-certificates
```

### Linux (Fedora/RHEL/CentOS)

```bash
# Extract the certificate from PFX
openssl pkcs12 -in chronicle-dev.pfx -clcerts -nokeys -out chronicle-dev.crt \
  -password pass:YourPassword123

# Copy to the trusted certificates directory
sudo cp chronicle-dev.crt /etc/pki/ca-trust/source/anchors/

# Update the certificate store
sudo update-ca-trust
```

## Docker Development

When running Chronicle in Docker, mount the certificate file:

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
      - ./chronicle-dev.pfx:/app/chronicle-dev.pfx:ro
    environment:
      - Cratis__Chronicle__Tls__CertificatePath=/app/chronicle-dev.pfx
      - Cratis__Chronicle__Tls__CertificatePassword=YourPassword123
```

## Client Configuration

When connecting to a Chronicle server with a self-signed certificate, configure the client:

```csharp
var options = new ChronicleOptions
{
    ConnectionString = "chronicle://localhost:35000",
    Tls = new Tls
    {
        CertificatePath = "./chronicle-dev.pfx",
        CertificatePassword = "YourPassword123"
    }
};

var client = new ChronicleClient(options);
```

## Security Considerations

⚠️ **Important**: The certificates generated in this guide are for **development only**.

- **Never use development certificates in production**
- **Do not commit certificates or passwords to source control**
- Use strong passwords for certificate protection
- Rotate certificates regularly
- For production, obtain certificates from a trusted Certificate Authority

## Troubleshooting

### Certificate Errors

If you encounter certificate validation errors:

1. **Verify the certificate path is correct**
   - Use absolute paths or paths relative to the working directory

2. **Check certificate password**
   - Ensure the password matches the one used during generation

3. **Verify certificate format**
   - Chronicle requires PFX (PKCS#12) format
   - Use `openssl pkcs12` to convert other formats

### Connection Errors

If the client cannot connect:

1. **Verify TLS is enabled on both server and client**
2. **Check that the server is listening on HTTPS**
3. **Ensure the client trusts the server certificate**
4. **Check firewall rules allow HTTPS traffic**

## Next Steps

- [Production Hosting](hosting/production.md) - Learn about production certificate requirements
- [Configuration](hosting/configuration.md) - Complete configuration reference
