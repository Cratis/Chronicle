# Cratis Chronicle

Chronicle is an event sourcing engine. It stores events as the source of truth and keeps read models — projections and reducers — up to date from them, so your application reads state that is derived rather than mutated in place. It ships as a server you run alongside your application, with client SDKs that connect to it.

- [Documentation](https://cratis.io)
- [Source](https://github.com/Cratis/Chronicle)

## Tags

Every tag below is published from the same `cratis/chronicle` repository. `latest` follows the most recent stable release; use a version tag in production.

| Tag | What it is |
|---|---|
| `<version>`, `latest` | The Chronicle server. Connects to a storage backend you provide. This is what you run in production. |
| `<version>-development`, `latest-development` | The server with MongoDB embedded and started for you as a single-node replica set. For local development only — the database lives inside the container and goes away with it. |
| `<version>-development-slim`, `latest-development-slim` | The development server without the embedded MongoDB, for when you bring your own database. |
| `<version>-workbench`, `latest-workbench` | The Workbench UI on its own, served by nginx. Only needed when hosting the UI separately — the server images already serve it. |

The server images are built for `linux/amd64` and `linux/arm64`. The workbench image is `linux/amd64`.

## Quick start

The development image is the fastest way to get something running — it needs nothing else:

```shell
docker run -p 127.0.0.1:35000:35000 cratis/chronicle:latest-development
```

The Workbench is then on <https://localhost:35000> — accept the self-signed development certificate — and clients connect to the same port. The `127.0.0.1:` prefix keeps the port on your machine: the development image accepts well-known credentials (`Admin` / `ChangeMeNow!`), so never expose it on a shared network.

The production image generates no certificates and has no default password. Point it at your own storage and give it a TLS certificate and an encryption certificate, or it stops at startup:

```shell
docker run -p 35000:35000 \
  -v /path/to/certs:/certs:ro \
  -e Cratis__Chronicle__Storage__Type=MongoDB \
  -e Cratis__Chronicle__Storage__ConnectionDetails=mongodb://host.docker.internal:27017 \
  -e Cratis__Chronicle__Tls__CertificatePath=/certs/chronicle.pfx \
  -e Cratis__Chronicle__Tls__CertificatePassword="$CHRONICLE_CERTIFICATE_PASSWORD" \
  -e Cratis__Chronicle__EncryptionCertificate__CertificatePath=/certs/encryption-cert.pfx \
  -e Cratis__Chronicle__EncryptionCertificate__CertificatePassword="$ENCRYPTION_CERTIFICATE_PASSWORD" \
  cratis/chronicle:latest
```

Set the initial administrator's password with `Cratis__Chronicle__Authentication__AdminUser__Password` from your secret store; otherwise the first visitor to the Workbench chooses it. The [production hosting guide](https://cratis.io/chronicle/hosting/production/) covers certificates, backups, and clustering.

> MongoDB storage requires a replica set — Chronicle uses transactions and change streams, neither of which a standalone `mongod` supports.

## Ports

| Port | Purpose |
|---|---|
| `35000` | Chronicle — client connections, the API, and the Workbench |
| `11111` | Orleans silo-to-silo communication, when running more than one instance |
| `30000` | Orleans gateway |

## Configuration

Configuration lives in `chronicle.json`. Every setting can be overridden with an environment variable by prefixing the path with `Cratis__Chronicle__` and separating levels with a double underscore.

| Environment variable | Default | Purpose |
|---|---|---|
| `Cratis__Chronicle__Storage__Type` | `mongodb` | Storage backend: `mongodb`, `postgresql`, `mssql`, `sqlite` or `inmemory` |
| `Cratis__Chronicle__Storage__ConnectionDetails` | `mongodb://localhost:27017` | Connection string for the backend |
| `Cratis__Chronicle__Port` | `35000` | Port Chronicle listens on |
| `Cratis__Chronicle__Authentication__Enabled` | `true` | Whether the Workbench and API require authentication |
| `Cratis__Chronicle__Authentication__AdminUser__Password` | | Initial password for the administrator; supply it from a secret store |
| `Cratis__Chronicle__Authentication__DefaultAdminPassword` | `ChangeMeNow!` | Development images only: the preset password for the initial admin user |
| `Cratis__Chronicle__Features__Api` | `true` | Whether the HTTP API is exposed |
| `Cratis__Chronicle__Features__Workbench` | `true` | Whether the Workbench UI is served |
| `Cratis__Chronicle__Tls__CertificatePath` | | Certificate used for TLS |
| `Cratis__Chronicle__Tls__CertificatePassword` | | Password for that certificate |
| `Cratis__Chronicle__EncryptionCertificate__CertificatePath` | | Certificate protecting the built-in OAuth authority's keys and values marked `[Encrypted]`; `[PII]` keys are managed separately |
| `Cratis__Chronicle__EncryptionCertificate__CertificatePassword` | | Password for that certificate |

## License

MIT.
