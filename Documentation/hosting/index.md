---
title: Cratis Chronicle hosting
description: Host the Cratis Chronicle Kernel with Docker Compose or Microsoft Aspire locally, then configure storage, certificates, and health probes for production.
---
Hosting Cratis Chronicle means running its Kernel, the server that stores events, builds read models through projections, and runs event handlers called observers. Your applications connect to this server to append events and query their state. Start with a local container, then configure durable storage and certificates before deploying the production image.

## Getting Started

Use [Docker Compose](docker-compose.md) to run the development image with its bundled MongoDB and an OpenTelemetry dashboard. If you already run your application through Microsoft Aspire, [add Chronicle to your AppHost](aspire) so Aspire manages its container and passes the endpoint to your application.

The development image generates a self-signed TLS certificate. You only need [Local Certificates](local-certificates) when you want to supply your own, for example to test certificate trust.

## Going to Production

Follow [Production hosting](production.md) to deploy the production image with external storage, certificates, and health probes. Unlike the development image, it has no bundled MongoDB and does not generate certificates.

TLS protects connections to Chronicle. A separate encryption-certificate setting protects stored ASP.NET Core Data Protection keys and supplies keys to Chronicle's internal OAuth authority, which issues authentication tokens. The internal authority requires this certificate in production. Using an external authority removes that startup requirement, but does not protect the stored keys: without an encryption certificate, Data Protection writes them unencrypted.

Before accepting traffic, configure [Data Protection Key Encryption](encryption-certificate.md) and include the encryption certificates and their passwords in your backup plan. You need those certificates to read protected data after a restore or a certificate rotation.

## All Hosting Topics

- [Production](production.md): Docker-based production deployment
- [Container Resources](container-resources.md): memory and garbage collection settings for the kernel image
- [Tracing Client Operations](client-tracing): send client spans to OpenTelemetry
- [Data Protection Key Encryption](encryption-certificate.md): configure, rotate, and back up encryption certificates
- [Configuration](configuration/index.md): server settings reference
- [Aspire](aspire): Microsoft Aspire hosting integration
- [Docker Compose](docker-compose.md): local development with telemetry
- [Local Certificates](local-certificates): supply a TLS certificate for development
