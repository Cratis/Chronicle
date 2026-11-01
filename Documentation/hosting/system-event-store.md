---
title: The System event store
description: Who can use the System event store, what is refused, and how a quarantined System observer recovers.
---

The System event store is available only to administrators.

Queries, commands and gRPC operations that address it are refused for every other caller, over both the Workbench
(HTTP) transport and gRPC. Registering projections, read models, reactors or reducers for it through
the API is refused for everyone, because the kernel manages its own definitions. Client SDKs pass store names through
unchanged, so a client registration that targets the System event store is refused as well.

Event store, namespace and identifier values that contain `#` are rejected before they reach storage.

## Upgrading

Enforcement is per node. During a rolling upgrade, nodes that have not yet been upgraded keep their earlier behaviour,
so complete the upgrade before relying on it.

## Recovery of a quarantined System observer

Restart the kernel. The kernel subscribes its own System reactors again at startup, and a fresh subscription ends the
observer's quarantine.

When the kernel uses an external authority, no caller is an administrator. In that setup the following System
operations are therefore unavailable until administrator mapping for external authorities is available:

- releasing the quarantine of a single partition;
- manually replaying or fast-forwarding a System observer.
