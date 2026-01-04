# Hosting

Chronicle provides multiple deployment options to suit different environments and requirements. Whether you're running in development, testing, or production environments, Chronicle offers flexible hosting solutions.

## Deployment Options

- **[Production](production.md)** - Docker-based production deployment with MongoDB
- **[Configuration](configuration.md)** - Complete configuration reference
- **[Local Certificates](local-certificates.md)** - Generate and configure TLS certificates for local development
- **Development** - Local development setup with MongoDB
- **Docker Compose** - Multi-container setup for development and testing

## Common Requirements

All Chronicle hosting environments require:

- **.NET 9 Runtime** - Chronicle is built on .NET 9
- **MongoDB** - Primary storage backend for events and projections
- **Network Access** - Chronicle exposes multiple ports for different services
- **Configuration** - See [Configuration](configuration.md) for details

## Architecture Overview

Chronicle operates as a distributed event store with the following components:

- **API Server** - REST API for client interactions
- **Event Store** - Core event storage and retrieval
- **Observer Engine** - Event processing and projections
- **Workbench** - Web-based management interface (optional)

Each component can be configured and scaled independently based on your hosting requirements.
