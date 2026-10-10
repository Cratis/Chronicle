---
id: 0002-event-generation-preservation-and-delivery
title: Preserve every event generation and deliver the generation each consumer pins
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-10
decider: woksin
applies-to:
  - Source/Kernel/Core/EventSequences/**
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Core/Projections/**
  - Source/Kernel/Core/Events/**
  - Source/Kernel/Storage/**
  - Source/Kernel/Storage.MongoDB/EventSequences/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/EventSequences/**
  - Source/Kernel/Storage.InMemory/EventSequences/**
  - Source/Kernel/Contracts/**
  - Source/Clients/DotNET/**
---

# Preserve every event generation and deliver the generation each consumer pins

## Context

An event type can have several generations, and Chronicle stores the event's content for every generation it can reach through the registered migrations. Issue #4189 found that consumers get the appended generation on live delivery but the highest stored generation on catch-up, replay, retries and reads. Investigating it showed more:

- The generation an event was appended in is not persisted.
- When a new generation is registered, the backfill job migrates each event from its highest stored generation and replaces the whole content map. The content the event was appended with can be overwritten by a downcast, which loses data for transforming migrations. The job also only covers the default namespace's event log.
- The kernel only releases (decrypts) the default content. A consumer that selects another generation receives protected values.
- "Highest stored generation" is the union of every client's registrations, so it can be newer than what a given consumer knows.

The research behind this record covered the kernel, storage, every client, and prior art (Marten, Axon, Akka, Avro and Confluent Schema Registry, CloudEvents). An independent review checked it against the code.

## Decision

1. **Storage keeps what was written.** Every stored event records the generation it was appended in. Stored generations are immutable originals; revisions are a separate dimension. Backfilling a new generation only adds content, migrating from the appended generation, with content and hash written together and guarded against concurrent revision and redaction. Migration definitions are versioned, and derived generations record the migration version that produced them.
2. **Existing events whose appended generation is unknown stay unknown.** The generation is never inferred. For these events, backfill migrates from the highest stored generation, the content that reads have returned so far, and records it as derived.
3. **Each consumer receives the generation it pins.** A typed observer pins a generation per event type when it registers, and "latest" is resolved and stored at that moment. On every path (live, catch-up, replay, partition retry, webhooks, outbox forwarding, projection joins) the kernel selects that generation's content and hash and releases it with that generation's schema. A generation that is not materialized yet is migrated for that delivery only. An unregistered generation fails explicitly instead of falling back to another schema.
4. **The appended generation is visible.** The event context carries it as an additive field, separate from the delivered generation.
5. **Revised events** are delivered from the revision, migrated to the pinned generation.
6. **One observer pins one generation per event type.** Registering handlers for two generations of the same event type in one observer is rejected.
7. **Rollout:** recording the appended generation and the add-only backfill ship first and change no behaviour. Pinned delivery ships as an opt-in minor release. It becomes the only behaviour in the next major release, which also removes `GenerationalContent` from observer delivery.

## Options considered

- **Appended generation everywhere:** keeps the information, but every client and the projection engine would have to select and decrypt generations themselves, and raw consumers would silently get older shapes. Rejected.
- **Newest generation everywhere:** the smallest change, but consumers that only know an older generation (rolling deploys, other services, other languages) break whenever the kernel knows a newer one. Rejected as the end state. Combined with item 1 it remains a valid interim state.
- **Reader-pinned generation, selected by the kernel:** chosen. The kernel is the only place that holds every generation, every migration and the encryption keys, and typed observer registrations already carry a generation per event type.

## Default if unanswered

Consumers keep seeing different generations depending on timing, the appended generation stays unknowable, and backfill keeps overwriting original content.

## Timeline and scope

Phases, tracked from #4189: (1) persist the appended generation; (2) add-only, guarded backfill with migration provenance, still limited to the default namespace's event log; (3) opt-in pinned delivery, including explicit policies for wildcard observers, captures, event store subscriptions, projections, reads, constraints and publications, and backfill for every namespace and sequence; (4) pinned delivery only, and `GenerationalContent` removed from observer delivery. No existing protobuf field numbers change.

## Verification

**Done when:** an event appended at any generation is delivered identically live and on replay to an observer pinned to any registered generation, with released content and the appended generation in its context, on all three storage providers; registering a new generation never changes existing stored content.

**Verify by:** storage specs for each provider, kernel specs for backfill guards and delivery selection, and client and integration specs for live versus replay parity, each phase gated by its own pull request.

## Consequences

Consumers can evolve independently: each declares the shape it understands, and the kernel translates. Audit and export tools can see what was actually appended. Delivery costs one selection and release per observer, plus a migration when a generation is not stored yet. Client code that selects generations itself becomes unnecessary and is removed in the major release.
