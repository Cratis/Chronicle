---
name: cratis-multi-service-architecture
description: Design a system of several Cratis services that each own a Chronicle event store - store per service on one kernel, namespace alignment, public contract events and the outbox, the three consumer routes, at-least-once delivery and idempotent consumers, personal data across stores, contract versioning, and what Arc leaves to the application. Use when splitting an application into services, connecting two event stores, or publishing and consuming events across services. Not for running the services locally (use cratis-multi-service-local-orchestration), tenancy inside one store (use cratis-chronicle-multi-tenancy), or Screenplay models (use cratis-screenplay-streams-and-consistency).
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-multi-service-architecture/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Several Cratis services, several event stores

A multi-service Cratis system is several applications, each owning one Chronicle
**event store** (the top-level container of its event sequences, event types,
observers and read models), talking to each other only through **public
contract events**. The producer publishes to its **outbox**; Chronicle forwards
each subscribed event into the consumer store's **inbox** for that producer
(`inbox-<producer store>`); the consumer translates it into its own private facts.

Requires Chronicle 19.36.0 or later (`dotnet list package`) for `[Public]`, kernel
visibility enforcement and publishing through an event-target projection. Older
clients register event types with unspecified visibility, which the kernel admits
to the outbox with a warning; upgrade before relying on the rules below.

## Decide these first

1. **One event store per service, one kernel for all of them.** A Chronicle kernel
   hosts many fully isolated stores. Forwarding between stores happens inside the
   kernel, so two services on different kernels cannot subscribe to each other;
   Chronicle provides no kernel-to-kernel link. Separate kernels need a transport
   the application owns (for example webhooks to an endpoint of the other service).
2. **A store is the service boundary; a namespace is a tenant inside it.** Do not
   model services as namespaces of one store, and do not model tenants as stores.
   Forwarding appends into the consumer's inbox **in the namespace with the same
   name** as the producer's, so every service must map a tenant to the identical
   namespace name. Arc's `TenantNamespaceResolver` maps the tenant id verbatim and
   an unset tenant to `Default`, which aligns automatically when every service
   resolves the same tenant id. Chronicle has no namespace mapping between stores.
3. **The store name is part of the contract.** The producer's configured
   `Cratis:Chronicle:EventStore` must equal the name in its contracts'
   `[EventStore("…")]`; that string is how the consumer's subscription finds the
   producer's outbox. Declare it once as a constant in the contracts project and
   never derive it from a deployment environment.
4. **Only public contract events cross.** Never share read models, private event
   types, a database or a collection between services. The producer translates
   private facts into a deliberate public summary event; the consumer translates
   that into its own private events (a Translation slice on each side).
5. **Ownership is a convention the client does not enforce.**
   `IChronicleClient.GetEventStore(name)` addresses any store on the kernel and a
   store comes into existence when a client first registers with it. A service
   appends only to its own store and its own outbox. Review for appends into another
   service's store or event log.

## Publish a contract

Put public events in a thin `<Service>.Contracts` project that references only
`Cratis.Chronicle` and carries the producer's store name once:

```xml
<ItemGroup>
  <AssemblyAttribute Include="Cratis.Chronicle.Events.EventStoreAttribute">
    <_Parameter1>Ordering</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

```csharp
// Ordering.Contracts/OrderAccepted.cs
using Cratis.Chronicle.Events;

namespace Ordering.Contracts;

[EventType]
public record OrderAccepted(decimal Total);
```

An `[EventStore]` on the type or its assembly registers the type as **public**;
`[Public]` does the same without naming a store. The kernel refuses a private
type in the outbox (`PrivateEventTypeCannotBeAppendedToOutbox`) and a public type
in the event log (`PublicEventTypeCannotBeAppendedToEventLog`); both arrive as
append-result errors, not exceptions, and refuse the whole batch.

Choose how the producer publishes:

| Default | When | Delivery behavior |
| --- | --- | --- |
| **Event-target projection**: `IProjectionFor<OrderAccepted>` built `From<OrderPlaced>` | The public event is a fold of private events | One atomic append per change, idempotent on retry; the outbox is the default destination |
| Reactor that observes a private event and appends to `EventSequenceId.Outbox` | The public event needs data a projection cannot fold | Re-runs on partition recovery, so it can append again |
| Command or service appending to the event log and then the outbox | Never | Two appends are not atomic: the private fact can stand without its publication |

Event-target targets cannot carry `[PII]` (`EventSequenceTargetCarriesCompliance`),
only one publisher may target a given event type per sequence
(`EventPublisherConflict`), and replaying the publishing projection appends **new**
public instances rather than rewriting history. Read
[references/publish-and-consume.md](references/publish-and-consume.md) when writing
the publishing projection or reactor, the consumer, an explicit subscription or a
CLI subscription.

## Consume a contract

Pick one route per source store:

| Route | Use when |
| --- | --- |
| **Implicit**: reference the producer's contracts package; observe its types in a reactor, reducer or projection | Default for .NET consumers. The client routes the observer to `inbox-<store>` and registers one kernel subscription per source store |
| **Explicit**: `eventStore.Subscriptions.Subscribe(id, "Ordering", b => b.WithEventType<OrderAccepted>())` and observe `[EventSequence(EventSequenceId.InboxPrefix + "Ordering")]` | Kotlin, TypeScript and Elixir consumers, contract types without `[EventStore]`, or a consumer that must choose types at run time |
| **Operator**: `cratis chronicle subscriptions add <id> <source-store> <type-ids> -e <target-store>` | Wiring stores that run no consumer code yet, or repairing a subscription by hand |

Implicit `[EventStore]` routing exists only in the .NET client. A producer marks a type public with
`[Public]` in .NET, or `use Chronicle.Events.EventType, public: true` in Elixir (Requires
Chronicle.Elixir 3.11.0). Every handled
type of one observer must come from one store (`CHR0008`), and an observer-level
`[EventStore]` cannot be combined with an explicit event sequence (`CHR0013`).
`cratis-chronicle-reactor` owns the routing order and those analyzers.

The consumer's first act is translation: append its **own private** event, then
build its read models from that. Never build a long-lived read model straight from
another service's contract; the contract then dictates the consumer's history.

## Delivery semantics the design must absorb

- **Order holds per event source only.** Observers process each event source's
  events in order; nothing orders events of different sources, or of different
  stores, relative to each other.
- **At least once.** Forwarding is a kernel observer of the producer's outbox; a
  failed inbox append fails the partition and is retried from the last forwarded
  event, and the inbox append carries no deduplication key. Producer replays add
  new public instances. Design every consumer to tolerate a repeated contract fact.
- **Idempotent consumers.** A `ReactorDelivery` handler parameter identifies one
  delivery of one inbox event (reactor, store, namespace, sequence, partition,
  sequence number) and is stable across retry, recovery and replay; keep a receipt
  under its `Id` before a side effect. It cannot recognize the same contract fact
  appended to the inbox twice, because each copy has its own sequence number. Where
  that would cause harm, make the translation idempotent on a business key the
  contract carries, or let a constraint refuse the duplicate local fact.
  `[OnceOnly]` only excludes replays.
- **Inbox history is the consumer's.** Replaying a consumer observer re-reads the
  inbox, not the producer. Unsubscribing leaves forwarded events in place and a
  re-subscription resumes from that point. Chronicle documents no way to re-forward
  producer history into an existing inbox, so a consumer that must rebuild from the
  producer's past needs an application-owned backfill.
- **Startup order is tolerant.** A subscription to a store that does not exist yet
  is parked and activates when the store appears.

## Personal data across stores

Forwarding preserves the event's subject and copies the subject's encryption key
into the consumer's store in the same namespace, unless that subject was erased
there. An erasure therefore reaches every store in the namespace. A forwarded
`[PII]` event for a subject erased in the consumer fails its partition. Give
every event that carries personal data an explicit subject, keep personal data out
of tags, and export only the fields consumers need. Read
`cratis-chronicle-compliance` for subjects and erasure.

## Versioning contracts

A public event type follows the same generation and migration rules as a private
one (`cratis-chronicle-event-type-migration`). Requires Chronicle 19.66.0 for
subscription-builder `WithGenerationDelivery(EventGenerationDelivery.Pinned)`
with typed `WithEventType<TEvent>()` or `WithEventType(id, generation)` to pin
forwarding into the target inbox. Both stores must register that generation;
Compatibility remains the default. Read
../cratis-chronicle-client-dotnet/references/pinned-generation-delivery.md when
configuring subscription pins and failure behavior. The CLI's `subscriptions add`
takes ids only; do not invent a generation flag. Prefer additive, optional changes
to a contract; publish a new
event type for a breaking change and keep publishing the old one until every
consumer has moved, rather than adding a generation consumers may not handle.
Version the contracts package with the contract, and never move a type between
stores.

## Arc in every service

- Each service is its own Arc host (`AddCratis()` or `AddCratisArc` plus
  `WithChronicle`) with its own `Cratis:Chronicle:EventStore` and its own read-model
  database. `cratis-arc-hosting` owns the bootstrap.
- Arc provides no server-side client for another service's commands or queries;
  generated proxies are for frontends. Prefer events between services. When a
  command truly needs another service's answer, fetch it in `Provide()` through an
  application-owned HTTP client and treat it as untrusted input.
- Arc does not propagate identity or tenant between services. A service calling
  another forwards the tenant header itself, authenticates as itself, and the callee
  enforces its own authorization. Forwarded identity details are not authorization
  evidence; see `cratis-arc-authentication-authorization-and-identity`.
- A frontend talking to several services: `<Arc microservice="…">` only adds routing
  metadata for an ingress the application configures, and nested `<Arc>` providers
  do not isolate identity or hubs. Prefer one frontend per service, or one ingress
  that routes by that metadata.

## Verify the system

1. `cratis chronicle event-stores list` shows one store per service and nothing
   named after an environment or a tenant.
2. `cratis chronicle subscriptions list -e <consumer-store>` shows one subscription
   per producer, with exactly the contract type ids the consumer handles.
3. A specification on each side: the producer publishes the contract from its
   private fact; the consumer turns one contract fact into its private fact, and a
   repeated delivery of it changes nothing.
4. One end-to-end run per tenant namespace: append on the producer, then observe
   the consumer's private event in the same namespace. Diagnose a stuck flow with
   `cratis chronicle diagnose --all-event-stores` (`cratis-chronicle-cli-operations`).

## Route near misses

- Run several services, a kernel and frontends together on one machine:
  `cratis-multi-service-local-orchestration`.
- Decide event vocabulary and stream boundaries inside one service:
  `cratis-chronicle-event-modeling`.
- Model the services, public events and Translate slices in Screenplay:
  `cratis-screenplay-streams-and-consistency`.
- Kernel production hosting, TLS and clustering: `cratis-chronicle-hosting`.
