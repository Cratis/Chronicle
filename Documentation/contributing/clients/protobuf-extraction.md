# Protobuf Extraction from C# Contracts

This document explains how to extract `.proto` files from C# gRPC service definitions using the ProtoGenerator tool.

## Overview

Chronicle uses a code-first approach for gRPC service definitions in C#. To support clients in other languages (TypeScript, Python, Go, etc.), we extract the service definitions to standard Protocol Buffer (`.proto`) files using the ProtoGenerator tool.

## ProtoGenerator Tool

**Location:** `Source/Tools/ProtoGenerator`

A .NET console application that generates `.proto` files from the Chronicle C# gRPC contracts.

### How It Works

1. Loads the `Cratis.Chronicle.Contracts.dll` assembly
2. Finds all interfaces decorated with `[Service]` attribute
3. Uses `protobuf-net.Grpc.Reflection` to generate proto files
4. Groups services by namespace to handle multiple packages
5. Outputs separate `.proto` files for each service namespace

### Usage

```bash
dotnet run --project Source/Tools/ProtoGenerator/ProtoGenerator.csproj -- \
  <path-to-contracts-dll> \
  <output-directory>
```

### Example

```bash
dotnet run --project Source/Tools/ProtoGenerator/ProtoGenerator.csproj -- \
  Source/Kernel/Contracts/bin/Release/net10.0/Cratis.Chronicle.Contracts.dll \
  Source/Kernel/Protobuf
```

## Generating Proto Files Locally

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

## Generated Proto Files

**Output location:** `Source/Kernel/Protobuf/*.proto`

The tool generates the following proto files:

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

## Using Proto Files for Other Clients

Once the proto files are generated, they can be used to generate client code in any language that supports gRPC:

### Python Example

```bash
python -m grpc_tools.protoc \
  -I Source/Kernel/Protobuf \
  --python_out=. \
  --grpc_python_out=. \
  Source/Kernel/Protobuf/*.proto
```

### Go Example

```bash
protoc \
  -I Source/Kernel/Protobuf \
  --go_out=. \
  --go-grpc_out=. \
  Source/Kernel/Protobuf/*.proto
```

### TypeScript Example

See [TypeScript gRPC Package](typescript-grpc-package.md) for details on the TypeScript client generation.

## Maintenance

When adding new gRPC services to the C# Contracts:

1. Decorate service interfaces with `[Service]` attribute
2. Decorate service methods with `[Operation]` attribute
3. Run the ProtoGenerator to regenerate proto files
4. Regenerate client code for all supported languages
5. Commit the updated proto files to the repository

## Dependencies

The ProtoGenerator tool uses:

- `protobuf-net.Grpc.Reflection` - For extracting proto definitions from C# attributes
- `.NET 10` - Runtime for the tool

## Proto File Format

The generated proto files follow the Protocol Buffers version 3 syntax and include:

- Service definitions with RPC methods
- Message types for requests and responses
- Import statements for dependencies (including `protobuf-net/bcl.proto` for .NET-specific types like Guid, DateTime, etc.)
