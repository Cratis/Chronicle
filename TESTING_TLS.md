# Testing TLS Implementation

This document describes how to verify the TLS implementation works correctly.

## Manual Testing

### 1. Test Server Startup with Dev Certificate

```bash
cd Source/Kernel/Server
dotnet run
```

Expected behavior:
- Server should start successfully
- Should listen on HTTPS (port 35000 by default)
- Should use ASP.NET Core development certificate (since placeholder is present)

### 2. Test Server with TLS Disabled

Create a `chronicle.json` file in `Source/Kernel/Server/`:

```json
{
  "DisableTls": true
}
```

```bash
cd Source/Kernel/Server
dotnet run
```

Expected behavior:
- Server should start successfully
- Should listen on HTTP (port 35000)

### 3. Test Client Connection

Create a simple test application:

```csharp
using Cratis.Chronicle;

// Test with TLS enabled (default)
var options = new ChronicleOptions
{
    Url = new ChronicleConnectionString("localhost:35000")
};

var client = new ChronicleClient(options);
var eventStore = await client.GetEventStore("test-store");

Console.WriteLine("Connected successfully with TLS!");

// Test with TLS disabled
var optionsNoTls = new ChronicleOptions
{
    Url = new ChronicleConnectionString("localhost:35000"),
    DisableTls = true
};

var clientNoTls = new ChronicleClient(optionsNoTls);
var eventStoreNoTls = await clientNoTls.GetEventStore("test-store");

Console.WriteLine("Connected successfully without TLS!");
```

### 4. Test with Real Certificate

1. Generate a self-signed certificate:
```bash
dotnet dev-certs https -ep certificate.pfx -p YourPassword123
```

2. Configure server to use it:
```json
{
  "CertificatePath": "certificate.pfx",
  "CertificatePassword": "YourPassword123"
}
```

3. Run the server and verify it uses the specified certificate.

### 5. Test Certificate Injection in Build

The certificate injection happens during the GitHub Actions workflow. To test locally:

1. Create a certificate file:
```bash
dotnet dev-certs https -ep shared-dev.pfx -p TestPassword
```

2. Encode it as base64:
```bash
base64 shared-dev.pfx > cert.txt
```

3. Manually inject into the build output:
```bash
cd Source/Kernel/Server
dotnet publish -c Release -o out
cat cert.txt | base64 -d > out/Certs/shared-dev.pfx
```

4. Run the published application:
```bash
cd out
./Cratis.Chronicle.Server
```

## Automated Testing

The existing tests should continue to pass. The TLS implementation uses default parameters that maintain backward compatibility:

```bash
dotnet test
```

## Verification Checklist

- [ ] Server starts with dev certificate when placeholder is present
- [ ] Server starts with HTTP when DisableTls is true
- [ ] Server uses specified certificate when CertificatePath is provided
- [ ] Client connects successfully with TLS enabled (default)
- [ ] Client connects successfully with TLS disabled
- [ ] Build completes successfully
- [ ] Existing tests pass
- [ ] Certificate injection works in GitHub workflows (verify in CI/CD)

## Security Summary

This implementation enhances security by:
- Enabling TLS by default for client-server communication
- Supporting custom certificates for production deployments
- Accepting development certificates for easy local development
- Providing fallback to ASP.NET Core dev certificates
- Not storing real certificates in the repository (using placeholders)
- Using GitHub Secrets for certificate injection in CI/CD

No security vulnerabilities were introduced - the implementation follows .NET security best practices for certificate handling and validation.
