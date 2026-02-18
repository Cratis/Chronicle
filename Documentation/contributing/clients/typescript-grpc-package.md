# TypeScript gRPC Package Generation

This document explains the TypeScript gRPC package generation and publishing setup for Chronicle.

## Overview

The Chronicle TypeScript gRPC package (`@cratis/chronicle.contracts`) provides strongly-typed TypeScript bindings for Chronicle's gRPC services. The package generation process consists of two main steps:

1. **Generate .proto files** from C# gRPC service definitions (see [Protobuf Extraction](protobuf-extraction.md))
2. **Generate TypeScript code** from proto files and publish the npm package

## Components

### 1. TypeScript Package (`Source/Clients/TypeScript`)

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
- Generates strongly-typed clients with full IDE support and IntelliSense
- Provides `ChronicleConnection` class for easy connection management
- Provides `ChronicleConnectionString` class for connection string parsing
- Implements OAuth 2.0 client credentials flow for authentication
- Exports all service clients and types
- Builds both ESM and CJS versions using Rollup

**Key classes:**
- `ChronicleConnection` - Main connection manager with service access
- `ChronicleConnectionString` - Connection string parser supporting `chronicle://` URI scheme
- `ChronicleServices` - Interface defining all available services
- `TokenProvider` - OAuth token management (client credentials and API key)

### 2. GitHub Workflows

#### Build Workflow (`.github/workflows/typescript-build.yml`)

Validates the TypeScript package on every PR and push to main.

**Triggers:**
- Push to main branch
- Pull requests to any branch
- Changes to TypeScript client, Contracts, Protobuf, or ProtoGenerator

**Steps:**
1. Setup .NET 10 and Node.js 23
2. Install protoc (Protocol Buffer Compiler)
3. Build the Contracts assembly
4. Run ProtoGenerator to generate .proto files
5. Install TypeScript dependencies
6. Generate TypeScript files from proto definitions using ts-proto
7. Build TypeScript package

#### Publish Workflow (`.github/workflows/publish.yml`)

Publishes the TypeScript package to NPM alongside .NET packages during releases.

**Triggers:**
- Release published

**Steps:**
1. Setup .NET 10 and Node.js 23
2. Install protoc
3. Build the Contracts assembly
4. Run ProtoGenerator to generate .proto files
5. Install TypeScript dependencies
6. Generate TypeScript files using ts-proto
7. Build TypeScript package
8. Publish to NPM
9. Commit and push updated proto and TypeScript files back to repository

**Required Secrets:**
- `NPM_TOKEN` - NPM authentication token for publishing

## Usage

### Prerequisites

Proto files must be generated first. See [Protobuf Extraction](protobuf-extraction.md) for details on generating proto files from C# contracts.

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
import { ChronicleConnection, ChronicleConnectionString } from '@cratis/chronicle.contracts';

// Using the Development connection string (includes default dev credentials)
const connection = new ChronicleConnection({
    connectionString: ChronicleConnectionString.Development
});

// Or using a custom connection string
const connection = new ChronicleConnection({
    connectionString: 'chronicle://my-client:my-secret@localhost:5000'
});

// Or using the legacy serverAddress (still supported)
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

### Authentication

The package supports two authentication methods:

**Client Credentials (OAuth 2.0):**
```typescript
// Connection string with credentials
const connStr = new ChronicleConnectionString('chronicle://client-id:client-secret@localhost:5000');

// Or using helper method
const connStr = ChronicleConnectionString.Default.withCredentials('client-id', 'client-secret');

const connection = new ChronicleConnection({ connectionString: connStr });
```

**API Key:**
```typescript
// Connection string with API key query parameter
const connStr = new ChronicleConnectionString('chronicle://localhost:5000?apiKey=my-api-key');

// Or using helper method
const connStr = ChronicleConnectionString.Default.withApiKey('my-api-key');

const connection = new ChronicleConnection({ connectionString: connStr });
```

**Custom Authority:**
```typescript
// If using an external OAuth server
const connection = new ChronicleConnection({
    connectionString: connStr,
    authority: 'https://auth.example.com'  // Defaults to Chronicle server if not specified
});
```

## Publishing a New Version

The TypeScript package is automatically published to NPM when a new GitHub release is created. The publish workflow will:

1. Generate fresh proto files from C# contracts
2. Generate TypeScript code from proto files
3. Build the TypeScript package
4. Publish to NPM with the release version tag
5. Commit updated proto and TypeScript files back to the repository

## Generated Files

**Proto files:** `Source/Kernel/Protobuf/*.proto`

See [Protobuf Extraction](protobuf-extraction.md) for the complete list of generated proto files.

**TypeScript files:** `Source/Clients/TypeScript/generated/*.ts`

The TypeScript generation creates:
- Service client interfaces and implementations
- Request and response message types
- Enum definitions
- Type definitions for all Chronicle concepts

## Maintenance

When adding new gRPC services to the C# Contracts:

1. Ensure services are decorated with `[Service]` attribute
2. Ensure methods are decorated with `[Operation]` attribute
3. Regenerate proto files (see [Protobuf Extraction](protobuf-extraction.md))
4. Run the TypeScript generation to create new TypeScript clients
5. The TypeScript package will automatically include the new services with full type safety
6. Publish a new version of the npm package by creating a GitHub release

## Package Structure

The published npm package includes:

- **ESM build** (`dist/esm/`) - ES modules for modern bundlers
- **CJS build** (`dist/cjs/`) - CommonJS for Node.js compatibility
- **Type definitions** (`dist/types/`) - TypeScript .d.ts files for IntelliSense
- **Source maps** - For debugging support
- **README.md** - Package documentation

## Dependencies

The TypeScript package has the following runtime dependencies:

- `@grpc/grpc-js` - gRPC client for Node.js
- `@grpc/proto-loader` - Protocol buffer loading (for metadata)

Development dependencies include:

- `ts-proto` - TypeScript code generation from proto files
- `typescript` - TypeScript compiler
- `rollup` - Module bundler for builds
- Various Rollup plugins for TypeScript and module resolution

