---
title: Access and recover Chronicle's System event store
description: Understand administrator access to Chronicle's System event store, its API registration limits and recovery of quarantined System observers.
---

With authentication enabled, Chronicle restricts the System event store to administrators. Use this page when a System-store request is refused or when a quarantined System observer needs recovery. The kernel owns this store's definitions; it is not a place to register your application's observers.

Queries, commands and gRPC operations targeting the System store require administrator access through both Workbench's HTTP transport and gRPC. See [administrator authorization](configuration/authentication.md#administrator-authorization) for the user and session requirements. When authentication is disabled, the administrator check allows access; disabling authentication is not a recovery procedure.

Even administrators cannot register projections, read models, reactors or reducers for the System store through the API. The kernel manages those definitions itself. Client SDKs pass store names through unchanged, so targeting the System store from a client does not bypass this restriction.

Event store, namespace and identifier values containing `#` are rejected before they reach storage.

## Upgrading

Enforcement is per node. During a rolling upgrade, nodes that have not yet been upgraded keep their earlier behavior. Complete the upgrade before relying on the restriction across the cluster, following the [administrator-authorization rollout](rolling-deploy.md#roll-out-administrator-authorization).

## Recovery of a quarantined System observer

A quarantine can stop one partition or the whole observer. At startup, the kernel re-registers its own System reactors, which releases an observer-level quarantine. Do not treat that registration as evidence that quarantined partitions have recovered.

With authentication enabled and an external authority configured, no caller qualifies as a Chronicle administrator. In that setup, you cannot release a single System partition's quarantine or manually replay or fast-forward a System observer. External-authority administrator mapping is not available.

If the problem is a missing active administrator rather than an observer quarantine, use the server-local recovery guidance in [administrator authorization](configuration/authentication.md#administrator-authorization).
