# TypeScript gRPC Package Generation

This document explains the TypeScript gRPC package generation setup for Chronicle.

## Overview

The Chronicle TypeScript gRPC package (`@cratis/chronicle.contracts`) provides TypeScript bindings for Chronicle's gRPC services. The package generation process consists of two main steps:

1. **Generate .proto files** from C# gRPC service definitions
2. **Build and publish** the TypeScript npm package

## Components

### 1. ProtoGenerator Tool (`Source/Tools/ProtoGenerator`)

A .NET console application that generates `.proto` files from the Chronicle C# gRPC contracts.

**How it works:**
- Loads the `Cratis.Chronicle.Contracts.dll` assembly
- Finds all interfaces decorated with `[Service]` attribute
- Uses `protobuf-net.Grpc.Reflection` to generate proto files
- Groups services by namespace to handle multiple packages
- Outputs separate `.proto` files for each service namespace

**Usage:**
```bash
dotnet run --project Source/Tools/ProtoGenerator/ProtoGenerator.csproj -- \
  <path-to-contracts-dll> \
  <output-directory>
```

**Example:**
```bash
dotnet run --project Source/Tools/ProtoGenerator/ProtoGenerator.csproj -- \
  Source/Kernel/Contracts/bin/Release/net10.0/Cratis.Chronicle.Contracts.dll \
  Source/Kernel/Protobuf
```

### 2. TypeScript Package (`Source/Clients/TypeScript`)

An npm package that provides strongly-typed TypeScript bindings for the Chronicle gRPC services.

**Structure:**
- `package.json` - Package configuration with @cratis/chronicle.contracts as the package name
- `tsconfig.json` - TypeScript compiler configuration
- `rollup.config.mjs` - Rollup bundler configuration for ESM and CJS builds
- `index.ts` - Main entry point that exports all generated services
- `ChronicleConnection.ts` - Connection manager for Chronicle services
- `ChronicleServices.ts` - Interface for all available services
- `generated/` - Directory containing TypeScript files generated from proto definitions
- `README.md` - Package documentation

**How it works:**
- Uses `ts-proto` to generate TypeScript files from proto definitions
- Generates strongly-typed clients with full IDE support
- Provides `ChronicleConnection` class for easy connection management
- Exports all service clients and types
- Builds both ESM and CJS versions using Rollup

### 3. GitHub Workflow (`.github/workflows/typescript-protobuf-build.yml`)

Automates the build and publish process.

**Triggers:**
- Manual dispatch with version input
- Push to main branch

**Steps:**
1. Setup .NET 10 and Node.js 23
2. Build the Contracts assembly
3. Run ProtoGenerator to generate .proto files
4. Install TypeScript dependencies
5. Generate TypeScript files from proto definitions using ts-proto
6. Build TypeScript package
7. Set package version (if manual dispatch)
8. Publish to NPM (if manual dispatch)
9. Commit and push proto and TypeScript files back to repository

**Required Secrets:**
- `NPM_TOKEN` - NPM authentication token for publishing

## Usage

### Generating Proto Files Locally

```bash
# Build the Contracts assembly
cd Source/Kernel/Contracts
dotnet build -c Release

# Generate proto files
cd ../../..
dotnet run --project Source/Tools/ProtoGenerator/ProtoGenerator.csproj -- \
  Source/Kernel/Contracts/bin/Release/net10.0/Cratis.Chronicle.Contracts.dll \
  Source/Kernel/Protobuf
```

### Building TypeScript Package Locally

```bash
cd Source/Clients/TypeScript
yarn install
yarn build
```

### Using the Published Package

Install the package:
```bash
npm install @cratis/chronicle.contracts
# or
yarn add @cratis/chronicle.contracts
```

Use in your TypeScript code:
```typescript
import { ChronicleConnection } from '@cratis/chronicle.contracts';

// Create a connection
const connection = new ChronicleConnection({
    serverAddress: 'localhost:5000'
});

// Connect to Chronicle
await connection.connect();

// Use the services with full type safety and IDE completion
const eventStores = await connection.eventStores.GetEventStores({});
console.log('Event stores:', eventStores.items);

// Access other services
const namespaces = await connection.namespaces.GetNamespaces({ EventStore: 'my-store' });

// Clean up
connection.dispose();
```

## Publishing a New Version

1. Go to GitHub Actions
2. Run "TypeScript Protobuf Build" workflow
3. Enter the version number (e.g., `1.0.0`)
4. The workflow will:
   - Generate proto files
   - Build the TypeScript package
   - Publish to NPM with the specified version
   - Commit updated proto files to the repository

## Generated Files

**Proto files location:**
- `Source/Kernel/Protobuf/*.proto` - Source proto files

**TypeScript files location:**
- `Source/Clients/TypeScript/generated/*.ts` - Generated TypeScript service clients

**Proto files:**
- `clients.proto` - Client connection services
- `cratis_chronicle_contracts.proto` - Event stores and namespaces
- `events.proto` - Event type services
- `events_constraints.proto` - Event constraint services
- `eventsequences.proto` - Event sequence services
- `host.proto` - Host/server information
- `identities.proto` - Identity services
- `jobs.proto` - Job management
- `observation.proto` - Observer services
- `observation_reactors.proto` - Reactor services
- `observation_reducers.proto` - Reducer services
- `projections.proto` - Projection services
- `readmodels.proto` - Read model services
- `recommendations.proto` - Recommendation services
- `seeding.proto` - Event seeding services

## Maintenance

When adding new gRPC services to the C# Contracts:

1. The services should be decorated with `[Service]` attribute
2. Methods should be decorated with `[Operation]` attribute
3. Run the ProtoGenerator to update proto files
4. Run the TypeScript generation to create new TypeScript clients
5. The TypeScript package will automatically include the new services with full type safety
6. Publish a new version of the npm package
