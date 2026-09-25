---
title: "Services"
description: "Where the kernel's gRPC service implementations live, and how the generated ones are produced."
---

All gRPC service implementations for our [contracts](./contracts) reside in the `Grpc` project (`Source/Kernel/Grpc`).

:::caution[New services are generated from Core, not written here]
You do not hand-write a new gRPC contract or its implementation. Write the operation as an Arc `[Command]` or `[ReadModel]` artifact in `Source/Kernel/Core`, attach it to a service with `[BelongsTo(WellKnownServices.<Service>)]`, and build Core. The build generates the contract interface and messages in `Source/Kernel/Contracts`, the implementation in `Source/Kernel/Grpc`, and the registrations in `Source/Kernel/Server/GeneratedGrpcServices.cs`. Never edit those generated files by hand.

What this page describes applies to the hand-written contracts that remain: services listed in `NonDerivedGrpcServices` in `Source/Kernel/Core/Core.csproj` (today `Observers`), and legacy contracts not yet converted.
:::

These services are owned by the `Kernel` and must remain internal. Every service implementation should
be marked as `internal`, since integration testing can host the client and kernel in the same process.
This is required by our [internalization](../clients/internalization.md) process for client assemblies.

Service implementations are built on top of the Grains exposed by the Kernel.
