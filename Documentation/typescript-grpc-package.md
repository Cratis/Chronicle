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

An npm package that provides TypeScript bindings for the Chronicle gRPC services.

**Structure:**
- `package.json` - Package configuration with @cratis/chronicle.contracts as the package name
- `tsconfig.json` - TypeScript compiler configuration
- `rollup.config.mjs` - Rollup bundler configuration for ESM and CJS builds
- `index.ts` - Main entry point that exports functions to load proto definitions
- `proto/` - Directory containing bundled .proto files
- `README.md` - Package documentation

**How it works:**
- Uses `@grpc/proto-loader` to load proto files at runtime
- Bundles proto files with the package for easy consumption
- Provides two functions:
  - `loadChronicleProtos()` - Loads from bundled proto files
  - `loadChronicleProtosFromPath(path)` - Loads from custom path
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
4. Copy proto files to TypeScript package
5. Install TypeScript dependencies
6. Build TypeScript package
7. Set package version (if manual dispatch)
8. Publish to NPM (if manual dispatch)
9. Commit and push proto files back to repository

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
import { loadChronicleProtos, grpc } from '@cratis/chronicle.contracts';

// Load proto definitions
const packageDefinition = loadChronicleProtos();

// Access services
const EventStores = packageDefinition.Cratis.Chronicle.Contracts.EventStores;

// Create client
const client = new EventStores(
  'localhost:5000',
  grpc.credentials.createInsecure()
);

// Call methods
client.GetEventStores({}, (error, response) => {
  if (error) {
    console.error('Error:', error);
    return;
  }
  console.log('Event stores:', response.items);
});
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
- `Source/Clients/TypeScript/proto/*.proto` - Bundled proto files for the npm package

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
4. The TypeScript package will automatically include the new services
5. Publish a new version of the npm package
