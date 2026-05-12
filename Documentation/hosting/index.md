# Hosting

Hosting Chronicle means running the **Chronicle Kernel** as a standalone server process. The Kernel is the heart of the system — it manages the event store, processes projections, coordinates observers, and exposes the API that client applications connect to. All event-sourced state flows through it, so the Kernel must be running before any client can append or query events.

Chronicle ships as a Docker image, making it straightforward to run anywhere containers are supported — locally for development, in CI, or in production clusters.

## Getting Started

For day-to-day development, [Docker Compose](docker-compose.md) is the fastest way to get a Kernel and MongoDB running locally. [Aspire](aspire.md) is an alternative if your team uses the .NET Aspire application model.

## Going to Production

Running the Kernel in production requires two things beyond a basic Docker deployment:

- **[Production](production.md)** — how to deploy the Chronicle Docker image with MongoDB, including port configuration, Docker Compose setup, health checks, and security considerations.
- **[Data Protection Key Encryption](encryption-certificate.md)** — Chronicle uses ASP.NET Core Data Protection to protect sensitive values at rest. In production you must supply a certificate so that data protection keys are encrypted. Without this, the Kernel will either fail to start or leave keys unprotected.

Both steps are required. A production deployment that skips key encryption is not secure.

## All Hosting Topics

- **[Production](production.md)** — Docker-based production deployment
- **[Data Protection Key Encryption](encryption-certificate.md)** — Certificate-backed key encryption for production
- **[Configuration](configuration/index.md)** — Complete configuration reference
- **[Aspire](aspire.md)** — Microsoft Aspire hosting integration
- **[Docker Compose](docker-compose.md)** — Multi-container setup for development and testing
- **[Local Certificates](local-certificates.md)** — TLS certificates for local development
