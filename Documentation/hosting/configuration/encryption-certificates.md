# Encryption Certificates

Chronicle uses `encryptionCertificate` to configure the certificate used by OpenIddict for encryption and signing keys when running with the internal OAuth authority.

When no certificate is configured:

- Development: Chronicle uses ephemeral keys.
- Production: Chronicle requires a configured certificate.

## Configuration

```json
{
  "authentication": {
    "authority": null
  },
  "encryptionCertificate": {
    "certificatePath": "/certs/encryption-certificate.pfx",
    "certificatePassword": "your-password"
  }
}
```

## Environment variables

```bash
Cratis__Chronicle__EncryptionCertificate__CertificatePath=/certs/encryption-certificate.pfx
Cratis__Chronicle__EncryptionCertificate__CertificatePassword=your-password
```

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| encryptionCertificate.certificatePath | string | null | Path to the certificate file (PFX format) |
| encryptionCertificate.certificatePassword | string | null | Password for the certificate file |

## Related topics

- [Authentication](authentication.md)
- [TLS](tls.md)
- [Workbench TLS Configuration](workbench-tls.md)
