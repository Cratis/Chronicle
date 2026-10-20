---
title: Kernel
description: How the Chronicle kernel is organised, how to build and test it locally, and where to start with contracts, services and patches.
---

The Chronicle kernel stores events, runs observers and processes projections. When contributing, start with the part of `Source/Kernel` that owns the behaviour you want to change, then work with its neighbouring specs.

## Find the implementation

- `Core` contains the Orleans grains and application behaviour, grouped into areas such as `EventSequences`, `Observation`, `Projections` and `Namespaces`.
- `Concepts` holds kernel domain types. `Contracts` defines the gRPC protocol, `Grpc` implements it, and `Protobuf` contains the generated protocol files.
- `Server` hosts the kernel and contains local configuration and infrastructure scripts.
- `Storage` defines storage abstractions; the `Storage.*` projects provide database, in-memory and encryption-key storage implementations.
- Projects ending in `.Specs` sit alongside the projects they test, including `Core.Specs`, `Server.Specs` and storage-provider specs.

For a new service operation, write the Arc command or read-model artifact in `Core` and attach it to a service with `BelongsTo`. Building Core generates contracts, service implementations and server registrations. Do not edit generated files by hand; the [contracts guide](./contracts.mdx) explains the conventions and the remaining hand-written contracts.

## Build, run and test locally

Use the .NET SDK selected by `global.json`. Running the local infrastructure also requires Docker with Compose. From the repository root, build with the flags used by CI:

```bash
dotnet build --configuration Release -p:DisableProxyGenerator=true -p:DisableDockerBuild=true
```

Start the development infrastructure and kernel with MongoDB, the default backend:

```bash
bash Source/Kernel/Server/run.sh
```

The script starts Compose services and runs the server. The kernel listens at `https://localhost:35000` with a self-signed development certificate. Use this setup only for local development: Compose exposes database ports and an anonymous diagnostics dashboard on the loopback interface.

Run the specs for the area you changed; for example, after the Release build:

```bash
dotnet test Source/Kernel/Core.Specs/Core.Specs.csproj --configuration Release --no-build
```

For checks that exercise the client and kernel together, follow [integration tests](../integration-tests.md).

## Contribution guides

- [Contracts](./contracts.mdx): protocol generation and compatibility conventions.
- [Services](./services.md): generated implementations and the services that remain hand-written.
- [Patches](./patches/index.mdx): versioned storage changes applied during kernel startup.
