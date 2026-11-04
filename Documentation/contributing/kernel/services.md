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

## Registration and startup

Client replicas can register the same read models concurrently during a restart. Keep registration mutations
serialized: making the entire manager reentrant would allow overlapping definition changes and writes.
The read-model and projection managers' `Ensure()` methods only activate their grains. They interleave with
registration because they neither read registration state nor promise that registration has completed.

Read-model registration compares schema content and indexes, not collection identity. Once a definition has
been persisted, accepted by its read-model grain, and its affected pipelines reconciled in this activation,
registering equal content again skips those operations. A fresh manager activation reconciles submitted
persisted definitions once; persisted metadata alone does not prove that a previous attempt finished.
Failed persistence, definition propagation, or eviction remains a registration failure and is retried.

This avoids redundant registration work; it does not clear failed observer partitions or guarantee that a
storage outage will recover. Diagnose those failures independently rather than interpreting successful
activation as successful registration or observer recovery.
