# Application Model Client

The Application Model client provides higher-level abstractions and patterns for building applications with Chronicle. It offers automatic dependency injection, event source id resolution, and simplified access to aggregate roots and read models within command handlers.

## Overview

The Application Model client is designed to work seamlessly with ASP.NET Core applications and provides:

- **Automatic Event Source ID Resolution**: Resolves event source identifiers from commands or generates new ones automatically
- **Aggregate Root Integration**: Automatic dependency injection of aggregate roots based on event source context
- **Read Model Access**: Direct access to read models through projections with automatic event source resolution
- **Command Response Handling**: Simplified patterns for returning events and values from command handlers

## Key Components

### Event Source ID Management

Learn how event source identifiers are resolved and managed within command contexts.

[Read more about Event Source ID →](event-source-id.md)

### Aggregate Roots

Discover how to take dependencies on aggregate roots and leverage event source context.

[Read more about Aggregate Roots →](aggregate-roots.md)

### Read Models

Understand how to access read models through projections with automatic resolution.

[Read more about Read Models →](read-models.md)

### Commands

Explore command handling patterns, including returning events and using response values.

[Read more about Commands →](commands.md)

## Getting Started

The Application Model client simplifies working with Chronicle by providing automatic dependency injection and higher-level abstractions.

[Get started with Application Model →](getting-started.md)
