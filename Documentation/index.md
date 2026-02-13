# Chronicle

Chronicle is a comprehensive event sourcing platform built with ease of use, productivity, compliance, and maintainability as core principles. It delivers a complete solution for implementing event-driven architectures in .NET applications through a powerful combination of server kernel and rich client SDK.

As an event sourcing database, Chronicle captures and stores all application state changes as a sequence of immutable events. Rather than persisting only current state, Chronicle preserves the complete history of system changes, unlocking powerful capabilities such as time travel debugging, comprehensive audit trails, and robust event-driven architectures.

## Artifacts

[![Nuget](https://img.shields.io/nuget/v/Cratis.Chronicle?logo=nuget)](http://nuget.org/packages/cratis.chronicle)
[![Docker](https://img.shields.io/docker/v/cratis/chronicle?label=Chronicle&logo=docker&sort=semver)](https://hub.docker.com/r/cratis/chronicle)

## Key Features

### 🏗️ Event Sourcing Foundation

- **Event Store**: Immutable storage of all domain events with full history preservation
- **Event Streams**: Organized sequences of events per aggregate or entity
- **Event Types**: Strongly-typed event definitions with schema evolution support
- **Event Metadata**: Rich contextual information including timestamps, correlation IDs, and custom tags

### 🎯 Real-time Processing

- **Observers**: React to events as they occur with guaranteed delivery
- **Projections**: Build read models and materialized views from event streams
- **Reducers**: Aggregate and transform event data into different representations
- **Reactors**: Execute side effects and integrate with external systems

### 🛡️ Enterprise Ready

- **Multi-tenancy**: Built-in support for isolated tenant data with namespaces
- **Constraints**: Ensure data integrity with custom validation rules
- **Security**: Authentication and authorization at multiple levels
- **Compliance**: Full audit trails and data lineage for regulatory requirements

### 🚀 Developer Experience

- **.NET Integration**: First-class C# SDK with strong typing and IntelliSense
- **Convention-based**: Minimal configuration with sensible defaults
- **Dependency Injection**: Native support for modern .NET DI patterns
- **Testing Support**: Comprehensive testing utilities and in-memory providers

### 📊 Operations & Monitoring

- **Management Dashboard**: Web-based interface for monitoring and administration
- **Health Checks**: Built-in health monitoring and diagnostics
- **Metrics**: Performance and operational metrics collection
- **Docker Support**: Container-ready with official Docker images

## Architecture

Chronicle follows a client-server architecture:

- **Chronicle Kernel**: The server component that manages event storage, processing, and querying
- **Client SDK**: .NET libraries that integrate with your applications
- **MongoDB Backend**: Optimized storage layer with support for other data stores through extensions
- **Web Dashboard**: Management interface for monitoring and administration

## Use Cases

Chronicle is ideal for applications that require:

- **Audit and Compliance**: Complete history of all changes with immutable records
- **Event-driven Microservices**: Reliable event-based communication between services
- **CQRS Implementation**: Separate command and query responsibilities with event sourcing
- **Analytics and Reporting**: Rich historical data for business intelligence
- **Debugging and Troubleshooting**: Time travel capabilities to understand system behavior
- **Integration Scenarios**: Event-driven integration with external systems

## Getting Started

Chronicle provides multiple ways to get started:

1. **Quick Start**: Run Chronicle with Docker and connect your .NET application
2. **Development Setup**: Local MongoDB instance with Chronicle kernel
3. **Production Deployment**: Scalable Docker-based deployment options

The platform supports both console applications and ASP.NET Core web applications with minimal setup required.

## Documentation Index

Explore Chronicle's comprehensive documentation organized by topic:

| Section | Description |
| ------- | ----------- |
| **[Get Started](./get-started/toc.yml)** | Quick start guides for console and ASP.NET Core applications |
| **[Concepts](./concepts/toc.yml)** | Core concepts including events, projections, observers, and constraints |
| **[Dynamic Consistency Boundary](./dynamic-consistency-boundary/toc.yml)** | Decision-scoped consistency and how Chronicle supports it |
| **[Clients](./clients/toc.yml)** | .NET client SDK documentation and application model guidance |
| **[Projections](./projections/index.md)** | How to create read models from events using projections |
| **[Hosting](./hosting/toc.yml)** | Deployment options including production and development environments |
| **[Configuration](./configuration/toc.yml)** | Client and server configuration guidance |
| **[Contributing](./contributing/toc.yml)** | Guidelines for contributing to Chronicle development |

---

Ready to get started? Check out our [Quick Start Guide](./get-started/index.md) or explore the [Core Concepts](./concepts/index.md) to understand Chronicle's powerful event sourcing capabilities.
