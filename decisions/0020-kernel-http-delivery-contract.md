---
id: 0020-kernel-http-delivery-contract
title: Deliver observer events to HTTP receivers through one versioned envelope, ordered per partition, identified by a deterministic delivery id, signed with Standard Webhooks, and bracketed by awaited replay boundaries
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-10-25
applies-to:
  - Source/Kernel/Core/Observation/Webhooks/**
  - Source/Kernel/Concepts/Observation/Webhooks/**
  - Source/Kernel/Contracts/Observation/Webhooks/**
  - Source/Kernel/Core/Observation/Jobs/HandleEventsForPartition.cs
  - Source/Kernel/Core/Observation/ReactorReplayHandler.cs
  - Source/Kernel/Core/Observation/ObserverEventPreparation.cs
  - Source/Kernel/Core/Configuration/Webhooks.cs
  - Source/Kernel/Core/Setup/WebhookHttpClientExtensions.cs
  - Source/Clients/DotNET/Webhooks/**
  - Documentation/webhooks/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Deliver observer events to HTTP receivers through one versioned envelope, ordered per partition, identified by a deterministic delivery id, signed with Standard Webhooks, and bracketed by awaited replay boundaries

Proposed on 2026-10-25. No decider is recorded yet; the record waits for a verdict on the [questions for the decider](#questions-for-the-decider).

This record settles the wire contract between the kernel and an HTTP receiver: what a request looks like, in which order and how often it arrives, how the receiver proves the kernel sent it, and how replays reach it. It is the design input for [#5115](https://github.com/Cratis/Chronicle/issues/5115), and the contract that [#5116](https://github.com/Cratis/Chronicle/issues/5116) (envelope, ordering, replay), [#5117](https://github.com/Cratis/Chronicle/issues/5117) (signatures) and the receivers in [#5119](https://github.com/Cratis/Chronicle/issues/5119), [#5120](https://github.com/Cratis/Chronicle/issues/5120) and [#5121](https://github.com/Cratis/Chronicle/issues/5121) build on, all under the serverless epic [#5104](https://github.com/Cratis/Chronicle/issues/5104).

It does not decide which transport owns an observer (stream or HTTP) or how an observer moves between them. That is [#5115](https://github.com/Cratis/Chronicle/issues/5115)'s ownership model and the cutover defect [#5370](https://github.com/Cratis/Chronicle/issues/5370). This record assumes exactly one transport serves an observer at a time.

## Context

### What exists today

The kernel already delivers to HTTP endpoints through webhooks. Webhook observers subscribe as `ObserverType.External` with `IWebhookObserverSubscriber` (`Source/Kernel/Core/Observation/Webhooks/Webhooks.cs` L240-L251).

| Concern | Behavior on the current tree | Where |
| --- | --- | --- |
| Body | `POST` of `JsonContent.Create(new EventsToObserve(partition, batch))`, so the content type is plain `application/json` and the body is `{partition, events}`. | `Source/Kernel/Core/Observation/Webhooks/WebhookMediator.cs` L54-L57 |
| Event shape | `events` is the kernel's internal `Concepts.Events.AppendedEvent` (context with `ObservationState`, content, and the public `OriginalContent`, `Revisions`, `GenerationalContent`, `GenerationalHashes` properties), serialized with the kernel's `JsonSerializerOptions`. Nothing pins this shape as a contract. | `Source/Kernel/Concepts/Observation/Webhooks/EventsToObserve.cs` L13, `Source/Kernel/Concepts/Events/AppendedEvent.cs` L13-L45 |
| Version | None. No version field, header or media-type parameter. | `WebhookMediator.cs` L56 |
| Delivery identity | None on the HTTP path. The gRPC stream contract has a stream-local `ulong DeliveryId` (zero means legacy) and a `ReplayState`; neither is sent over HTTP. | `Source/Kernel/Contracts/Observation/EventsToObserve.cs` L26-L36 |
| Acknowledgement | Any 2xx acknowledges the whole batch; no response body is read. | `WebhookMediator.cs` L59-L65 |
| Response classification | `429`/`503` with a valid `Retry-After` defer the partition; `410` parks it; every other `4xx` except `408` and `429` pauses the observer (including `413`); everything else is a transient failure. A circuit-open rejection defers the partition. | `WebhookMediator.cs` L67-L75, L118-L143; `Source/Kernel/Core/Observation/WebhookDeliveryFailurePolicy.cs` |
| Retry and park | One HTTP attempt per delivery; the observer owns durable retries. Defaults: schedule 1 s, 5 s, 30 s, 2 min, 10 min, 30 min, then hourly with jitter; no attempt-count limit; 24-hour budget; then park; observer quarantine at 5 parked partitions or 50%. Shipped through [#5108](https://github.com/Cratis/Chronicle/issues/5108) and documented in `Documentation/webhooks/index.mdx`. | `Source/Kernel/Core/Setup/WebhookHttpClientExtensions.cs` L39-L65; `Source/Kernel/Core/Configuration/Webhooks.cs` L17, L78-L85 |
| Timeouts | Webhook subscribers are not retried immediately after a timeout; the job yields and recovery belongs to the observer. Abandoned deliveries are held by delivery fences until the remote call can no longer land. | `Source/Kernel/Core/Observation/Jobs/HandleEventsForPartition.cs` L239-L250; `Source/Kernel/Core/Observation/DeliveryFences.cs` |
| Authentication | Basic, Bearer, OAuth client credentials, managed identity, or none, plus static target headers. No request signing. | `Source/Kernel/Core/Observation/Webhooks/WebhookHttpClientFactory.cs` L35-L71; `Source/Kernel/Concepts/Observation/Webhooks/WebhookAuthorization.cs` L13 |
| Replay signal | Only the per-event `ObservationState` flag (`Initial = 1`, `Replay = 2`). `ReactorReplayHandler` sends replay begin and end only for `ObserverType.Reactor`, so webhook observers get no boundary at all, and for stream reactors the notification is fire-and-forget. | `Source/Kernel/Concepts/Events/EventObservationState.cs` L20-L25; `Source/Kernel/Core/Observation/ReactorReplayHandler.cs` L26, L90 |
| Replay trigger | Changing a webhook definition replays every namespace when `ReplayOnDefinitionChange` is set, otherwise records a replay recommendation. | `Source/Kernel/Core/Observation/Webhooks/Webhook.cs` L53-L59 |
| Redaction | A redaction is delivered as a `GlobalEventTypes.Redaction` event to observers that subscribe to the redacted event's original type. | `Source/Kernel/Core/Observation/ObserverEventPreparation.cs` L38-L60 |
| Generations | Webhooks can pin event generations; pinned events are released to the pinned generation before delivery, per [decision 0002](0002-event-generation-preservation-and-delivery.md). | `Source/Kernel/Core/Observation/Webhooks/WebhookObserverSubscriber.cs` L112-L145 |

### What the spike measured

The local spike for [#5114](https://github.com/Cratis/Chronicle/issues/5114) (closed on local evidence; its findings are in the comments on [#5104](https://github.com/Cratis/Chronicle/issues/5104), [#5115](https://github.com/Cratis/Chronicle/issues/5115) and [#5116](https://github.com/Cratis/Chronicle/issues/5116)) ran one local Release kernel with SQLite and a local HTTP receiver. Relevant findings:

- The body carries `partition` and `events` only. There is no delivery id, no replay state and no deadline header.
- A `500` redelivers the whole ordered batch: three events and four failed requests gave twelve calls, three effects and nine duplicates the receiver had to drop.
- A handler that ignored a 2-second deadline received one event 25 times, up to 22 at once. This is filed as [#5369](https://github.com/Cratis/Chronicle/issues/5369) (open). Kernel-side cancellation cannot stop work the receiver has already started.
- A replay arrived as one ordered batch with the events flagged as replayed. No replay-started or replay-ended request exists, and receivers cannot infer replay boundaries from batch edges.
- Registering a webhook for an observer that an open gRPC stream serves returned success, but the stream kept the observer ([#5370](https://github.com/Cratis/Chronicle/issues/5370)).
- Not measured: platform cold starts, multiple receiver instances, interleaving of live and replay deliveries, cross-partition order, and any wire test of authentication headers.

### What the epic already proposes

The epic [#5104](https://github.com/Cratis/Chronicle/issues/5104) records sixteen decisions in its description; they are issue text, not accepted decision records. The ones this contract depends on are: the guarantee wording "at-least-once, ordered per partition except after a delivery timeout" (1), retry and park defaults for HTTP targets (10), vendor-specific authentication deferred (11), Ed25519 `v1a` as the default signature with HMAC `v1` for generic receivers (12), and a native v1 envelope for Cratis receivers with CloudEvents later and optional for generic webhooks (9). [#5116](https://github.com/Cratis/Chronicle/issues/5116) and [#5117](https://github.com/Cratis/Chronicle/issues/5117) spell these out in detail. Note that managed identity (an Entra ID token) already exists on the webhook path, so decision 11 is partly overtaken by the code.

### Why it needs a record

Every receiver built after this point, including the client receiver core and two platform adapters, encodes the contract. Once receivers exist outside the repository, any change to the body, the identity rules or the replay signaling is a new major version of the contract. If nobody chooses, the de facto contract stays the serialization of an internal kernel record.

### Why one record

The skill this repository follows asks that one record settle one question. The question here is "what does an HTTP delivery promise a receiver", and the five areas are parts of one answer: the envelope version covers the body, the headers, the response semantics and the replay kinds together, and a receiver must implement all of them at once to be correct. Splitting them would let one part change without bumping the version the others rely on.

Two parts are separable and are proposed as a split (see [Q1](#q1-one-record-or-a-split)):

- **Signing key custody**: key scope, generation, storage, rotation procedure and public-key distribution. This is an operational security lifecycle rather than a wire shape; the wire part (which headers, which algorithms, what is signed) stays here.
- **Transport ownership and cutover**: already owned by [#5115](https://github.com/Cratis/Chronicle/issues/5115) and [#5370](https://github.com/Cratis/Chronicle/issues/5370), and kept out of this record.

## Decision

The kernel delivers events to Cratis HTTP receivers as `POST` requests with the content type `application/vnd.cratis.chronicle.delivery+json; v=1`, whose body is a versioned envelope of kind `events`, `replay-started`, `replay-ended` or `ping`, carrying the observer, event store, namespace, event sequence, partition, delivery id and events in a pinned public shape. Delivery is at least once and ordered per (observer, namespace, partition), with at most one request in flight per partition except for the bounded overlap after a timeout, and with per-target concurrency caps that include replay. Each request carries a deterministic `webhook-id` that is identical across retries of the same batch and different for a replay of the same range, together with `webhook-timestamp`, a `webhook-signature` per the Standard Webhooks specification (Ed25519 `v1a` by default, HMAC `v1` where a receiver only supports shared secrets), a deadline header and `traceparent`. Retry, deferral, park and quarantine follow the policy shipped in [#5108](https://github.com/Cratis/Chronicle/issues/5108), extended with partial acknowledgement and payload limits. A replay is bracketed by `replay-started` and `replay-ended`, and the kernel dispatches no replayed partition before the receiver has acknowledged `replay-started`. Generic webhooks keep today's body until they opt in.

### Envelope

- Media type `application/vnd.cratis.chronicle.delivery+json; v=1`. A receiver that does not support the version answers `415`, which pauses the observer as a configuration error (today's classification).
- Body fields: `kind`, `observerType`, `observerId`, `eventStore`, `namespace`, `eventSequenceId`, `partition`, `deliveryId`, `events[]`, and `initialState` for reducers (from [#5116](https://github.com/Cratis/Chronicle/issues/5116)).
- Events use a pinned public shape defined in the contracts, not the kernel's `Concepts.Events.AppendedEvent`. Each event carries its context (including sequence number, event type with the delivered generation, observation state, correlation and causation) and its content. Internal storage fields (`OriginalContent`, `GenerationalContent`, `GenerationalHashes`) are not part of v1.
- Unknown fields are ignored by receivers; adding a field is not a version change. Removing or renaming a field, or changing a semantic, is.

### Ordering and concurrency

- One request in flight per (observer, namespace, partition). A retry never starts while the previous request for the partition runs, except that a request abandoned after its deadline may still be executing at the receiver; the overlap is bounded by the deadline plus the backoff delay. Kernel-side, the abandoned delivery stays behind its delivery fence.
- Partitions run in parallel up to a per-observer limit, and a per-target-authority limit applies across live delivery, catch-up, retries and replay. Proposed defaults are 8 and 16 ([#5116](https://github.com/Cratis/Chronicle/issues/5116)).
- No order is promised across partitions or across observers.
- An HTTP delivery is a writer in the sense of [decision 0018](0018-partition-ownership-protocol.md): it delivers only while it holds the partition claim. If [decision 0004](0004-ordered-lanes-observer-delivery.md) (proposed) is accepted, its sequencing key replaces the partition as the ordering unit without changing the envelope.

### Idempotency, acknowledgement and retries

- `webhook-id` is a deterministic hash of (event store, namespace, observer, partition, first sequence number, last sequence number, kind, replay epoch). The replay epoch is a counter the observer increments on each replay, so a replay is never deduplicated against the original delivery. The same batch on retry carries the same id.
- Receivers deduplicate side effects per event on (observer, namespace, sequence number), not per request, because a partial acknowledgement or a halved batch changes batch boundaries. Reactor side-effect appends are keyed `{observerId}:{sourceSequenceNumber}` once the client exposes keyed appends (epic decision 1; [decision 0012](0012-keyed-appends-travel-on-their-own-rpcs.md)).
- `2xx` with an empty body or `204` acknowledges the whole batch. `200` with a JSON result `{state, lastSuccessfulObservation, exceptionMessages, exceptionStackTrace, readModelState}` acknowledges up to `lastSuccessfulObservation` and fails from the next event.
- `413` halves the batch instead of pausing the observer; a single event over the size limit parks the partition with reason `PayloadTooLarge`. Batches are bounded by an event count and a byte size (proposed 100 events and 4 MiB).
- All other classifications, the retry schedule, the 24-hour budget, park and quarantine stay as shipped in [#5108](https://github.com/Cratis/Chronicle/issues/5108) and documented in `Documentation/webhooks/index.mdx`.
- A `Cratis-Delivery-Deadline` header (UTC) tells the receiver when the kernel abandons the request. The platform's request timeout must be at most the delivery timeout.

### Authentication and signatures

- Every envelope delivery is signed per Standard Webhooks: `webhook-id`, `webhook-timestamp` (Unix seconds) and `webhook-signature` over `id.timestamp.body`. The default is Ed25519 (`v1a`), so a receiver holds only a public key; HMAC-SHA256 (`v1`) is available for receivers that only support shared secrets. Several signatures can be sent at once so keys can rotate with an overlap.
- Receivers verify the signature in constant time, reject timestamps outside a tolerance (proposed 5 minutes), and never follow redirects.
- The existing transport authentication modes stay available and combine with signing: Basic, Bearer, OAuth client credentials, managed identity and static headers (for example a platform function key). `None` without a signature requires an explicit `AllowUnauthenticated` setting and logs a startup warning ([#5117](https://github.com/Cratis/Chronicle/issues/5117)).
- Signing is opt-in for generic webhooks, which keep today's body.
- Key custody (scope, storage under the kernel's `IEncryption`, rotation, distribution of the public key) is either decided here as proposed by [#5117](https://github.com/Cratis/Chronicle/issues/5117) (one key per event store, rotated through the CLI, returned by registration and exposed by a kernel `GET`) or split into its own record ([Q1](#q1-one-record-or-a-split), [Q9](#q9-signing-key-scope-and-rotation)).

### Replay, catch-up and redaction

- Before the first replayed partition the kernel sends `replay-started` and waits for a `2xx`; a non-`2xx` backs off and retries instead of dispatching. After the last partition it sends `replay-ended`. Both carry the replay epoch.
- Replayed events keep the `Replay` observation flag. Catch-up after downtime is not a replay: it is delivered as ordinary `events` deliveries with no boundary.
- A redaction is delivered as an ordinary event of the redaction type, as today. Revised events are delivered from the revision, migrated to the pinned generation, per [decision 0002](0002-event-generation-preservation-and-delivery.md). Content is sent in plaintext after compliance release over HTTPS (epic decision 6); encrypted or redacted delivery is [#5132](https://github.com/Cratis/Chronicle/issues/5132).

## Options considered

### Envelope

- **Keep today's `EventsToObserve` JSON.** No work, and existing webhook receivers keep working. But it is the serialization of an internal kernel record with no version, it has no delivery id, observer, namespace or replay information, and any internal refactor silently changes the wire. Kept only for generic webhooks that do not opt in.
- **CloudEvents (structured or batched).** A recognized standard with broker and platform tooling. It carries one event per envelope and has no native place for partition, delivery id, replay boundaries or reducer state, so all of those become extensions every receiver must still understand. Kept as a later, optional format for generic webhooks (epic decision 9).
- **Protobuf over HTTP, reusing the stream contract.** One shape for both transports, and the stream contract already has `DeliveryId` and `ReplayState`. But function platforms and generic tooling handle JSON far better, the stream `DeliveryId` is stream-local and means nothing over HTTP, and debugging a binary body from a function log is harder. Rejected.
- **Native versioned JSON envelope.** Chosen. It carries exactly what a receiver needs and is versioned from the first release. The cost is a contract the project owns and must document.
- **Where the version lives.** Media-type parameter (chosen; content negotiation and `415` already work for it), a body field (needs the body parsed before the version is known), a custom header (easy to strip at a proxy) or the URL path (couples the receiver's routing to the contract). A body field may be added for logging, but the media type is authoritative.

### Ordering

- **One request in flight per observer.** Simplest and fully ordered, but a single slow partition stalls every other key, and throughput is bounded by one function invocation at a time. Rejected.
- **No per-partition limit.** Maximum throughput, but breaks the per-partition ordering every Chronicle observer promises and reproduces the overlap seen in [#5369](https://github.com/Cratis/Chronicle/issues/5369). Rejected.
- **One in flight per partition with per-observer and per-target caps.** Chosen. It matches stream delivery's ordering promise and bounds the load a replay puts on a receiver's concurrency quota.

### Delivery identity

- **A random id per attempt.** Easy, but useless for deduplication: every retry looks new.
- **The stream's `DeliveryId`.** A stream-local counter that resets with the stream; zero means legacy. Not stable across retries, kernels or transports. Rejected.
- **A deterministic hash without a replay component**, as written in [#5117](https://github.com/Cratis/Chronicle/issues/5117). Stable across retries, but a replay of the same range produces the same id as the original delivery, so a receiver deduplicating on `webhook-id` drops the replay. Rejected.
- **A deterministic hash including a replay epoch.** Chosen. Stable across retries, distinct for a replay.

### Acknowledgement

- **Whole-batch acknowledgement only (today).** No receiver work, but a failure on the last event redelivers every earlier event, which the spike measured as nine duplicates for three effects.
- **Partial acknowledgement through a JSON result.** Chosen. It mirrors `ObserverSubscriberResult` on the stream path and lets the kernel advance to the last successful event. The receiver must still deduplicate, because a lost response looks like a failure.

### Signatures and authentication

- **Transport authentication only (today).** Works for webhooks behind an authenticated gateway. A function exposed with no platform authentication (for example a Lambda Function URL with `AuthType NONE`) then has nothing to verify, and a bearer secret must be distributed to every function. Rejected as the default for HTTP reactors and reducers.
- **HMAC `v1` only.** Widest receiver library support, including Svix verifiers. Every receiver holds a secret that can forge deliveries. Kept as an alternative, not the default.
- **Ed25519 `v1a` by default.** Chosen. A leaked function configuration cannot forge deliveries. The cost is key lifecycle in the kernel, and verifier support for `v1a` in the .NET library named by [#5117](https://github.com/Cratis/Chronicle/issues/5117) is unverified.
- **HTTP Message Signatures (RFC 9421) or JWS.** Standards with broader scope, but less receiver tooling in the webhook ecosystem and no ready verifier in the platforms targeted. Rejected.
- **Mutual TLS.** Strong, but not available on the managed HTTP fronts of Azure Functions and Lambda Function URLs without extra infrastructure. Rejected as a default.
- **Cloud IAM (AWS SigV4, Entra ID).** Native to each platform but different on each. Deferred by epic decision 11, except that the managed identity mode already exists for webhooks.

### Replay signaling

- **The per-event `Replay` flag only (today).** No new requests, but a receiver cannot tell when a replay starts (to reset state) or ends (to resume side effects), and over HTTP the start and the first partition can land on different instances.
- **Fire-and-forget begin and end, as on the stream.** Already shown to race on the stream path ([#5116](https://github.com/Cratis/Chronicle/issues/5116)); over HTTP the risk is larger. Rejected.
- **Awaited `replay-started` and `replay-ended`.** Chosen. One extra request per replay; partitions wait on an acknowledgement.
- **Per-partition replay boundaries as well.** The stream handler has `BeginReplayPartitionFor` and `EndReplayPartitionFor` (`ReactorReplayHandler.cs` L57-L88). Left open ([Q11](#q11-replay-boundaries)).

## Default if unanswered

HTTP receivers keep receiving the serialization of `Concepts.Events.AppendedEvent` in a `{partition, events}` body, with no version, no delivery id, no signature beyond transport authentication, whole-batch acknowledgement and no replay boundary. The cost:

- [#5116](https://github.com/Cratis/Chronicle/issues/5116), [#5117](https://github.com/Cratis/Chronicle/issues/5117), [#5119](https://github.com/Cratis/Chronicle/issues/5119), [#5120](https://github.com/Cratis/Chronicle/issues/5120) and [#5121](https://github.com/Cratis/Chronicle/issues/5121) cannot start without guessing, and the serverless stage of [#5104](https://github.com/Cratis/Chronicle/issues/5104) stays blocked.
- Any receiver written meanwhile binds to an internal shape, so introducing a contract later is a breaking change for those receivers.
- Receivers must deduplicate every event of every redelivered batch and cannot distinguish replays from redeliveries except by the per-event flag.
- Functions exposed without platform authentication cannot verify that a request came from the kernel.

## Timeline and scope

**Horizon:** envelope v1 holds until a v2 is decided by a superseding record; v1 stays supported for at least one major release after v2 ships.

**Order of delivery**, following [#5116](https://github.com/Cratis/Chronicle/issues/5116)'s five parts after the ownership work in [#5115](https://github.com/Cratis/Chronicle/issues/5115):

1. Signing headers on existing webhooks, opt-in ([#5117](https://github.com/Cratis/Chronicle/issues/5117)); additive, can ship first.
2. Envelope v1, deadline header and `traceparent`, opt-in per target.
3. Per-partition and per-target concurrency caps, including replay.
4. Partial acknowledgement, `413` halving and batch size limits.
5. Awaited replay handshake.
6. Reducer state round trip.

**In scope:** the request and response contract, headers, delivery identity, ordering and concurrency promises, acknowledgement and response classification for envelope deliveries, signature scheme on the wire, replay and redaction signaling, and the receiver guidance that follows from them.

**Out of scope:**

- Which transport owns an observer, conflict rules and cutover ([#5115](https://github.com/Cratis/Chronicle/issues/5115), [#5370](https://github.com/Cratis/Chronicle/issues/5370)).
- Registration, artifact fingerprints and deployment generations ([#5111](https://github.com/Cratis/Chronicle/issues/5111)).
- The retry schedule and park policy themselves, already shipped by [#5108](https://github.com/Cratis/Chronicle/issues/5108); this record only adopts them.
- Pull-based delivery for receivers the kernel cannot reach ([#5125](https://github.com/Cratis/Chronicle/issues/5125)).
- Encrypted or redacted delivery of personal data ([#5132](https://github.com/Cratis/Chronicle/issues/5132)).
- Broker sinks that reuse the envelope fields ([#5134](https://github.com/Cratis/Chronicle/issues/5134), [#5135](https://github.com/Cratis/Chronicle/issues/5135)).
- Vendor IAM signing (SigV4, Entra invocation).
- Key custody, if the decider takes the split in [Q1](#q1-one-record-or-a-split).

## Verification

**Done when:**

- A contract specification in the kernel pins the v1 envelope for each kind (`events`, `replay-started`, `replay-ended`, `ping`) against a checked-in example, and every envelope delivery carries the v1 media type, `webhook-id`, `webhook-timestamp`, `webhook-signature`, `Cratis-Delivery-Deadline` and `traceparent`.
- A specification shows `webhook-id` identical across retries of one batch and different for a replay of the same range.
- Fault-injection specifications show never more than one request in flight per partition outside the documented timeout window, and in-flight requests to one target authority never above the cap during a replay with several partitions.
- Specifications cover `204`, `200` with a partial result, `413` halving, an oversized single event parking with `PayloadTooLarge`, and the existing `429`, `410` and `4xx` classifications unchanged.
- A specification shows no replayed partition dispatched before `replay-started` is acknowledged, and `replay-ended` sent after the last partition.
- An interoperability specification verifies an HMAC `v1` delivery with a published Standard Webhooks verifier and an Ed25519 `v1a` delivery with the receiver's verifier; a modified body, an expired timestamp and a wrong key are rejected.
- Existing generic webhooks without opt-in receive the same body and headers as before (regression specification).
- `Documentation/webhooks/` describes the receiver contract, the guarantee wording and the deduplication guidance.

**Verify by:** the kernel specification suites under `Source/Kernel/Core.Specs` and the client receiver suites run in the `dotnet-build` workflow, the interoperability specification passing against the named verifier libraries, and a local run of one HTTP reactor (as in [#5114](https://github.com/Cratis/Chronicle/issues/5114)) showing the envelope, signature headers and replay handshake on the wire.

## Consequences

**Easier:** receivers in any language can be written against a documented, versioned contract; the .NET receiver core and the platform adapters share one verification and dispatch path; operators can correlate a request with an observer, partition and replay from headers alone; broker sinks can reuse the same identity fields.

**Harder:** the project owns a public wire contract and its compatibility; the kernel gains signing key lifecycle; replays wait on a receiver acknowledgement, so an unreachable receiver delays a replay instead of letting it race; partial acknowledgement adds a response parser to the delivery path.

**Foreclosed:** sending the kernel's internal event record over HTTP to Cratis receivers; fire-and-forget replay notification over HTTP; unsigned envelope deliveries unless an operator explicitly allows them.

## Questions for the decider

Each question has a recommendation; the record can be accepted with the recommendations unchanged.

### Q1. One record or a split

Should this record also settle signing key custody (scope, storage, rotation, public-key distribution), or should that move to its own record, with this one keeping only the wire signature scheme? **Recommendation:** split key custody into a follow-up record; keep the wire scheme here. Transport ownership stays with [#5115](https://github.com/Cratis/Chronicle/issues/5115) in either case.

### Q2. Envelope format and event shape

Native JSON with `application/vnd.cratis.chronicle.delivery+json; v=1`, and events in a pinned public shape rather than the kernel's internal record? Which internal fields (`OriginalContent`, `GenerationalContent`, `GenerationalHashes`, `Revisions`) are excluded from v1? **Recommendation:** yes, and exclude all four; the delivered generation and the revision are already resolved by the kernel.

### Q3. Generic webhooks

Do existing webhooks keep today's body indefinitely, move to envelope v1 by opt-in, or switch by default in the next major release? **Recommendation:** opt-in now, default in the next major release, with the old body available behind a setting for one more major.

### Q4. Ordering unit and concurrency defaults

Is the ordering unit (observer, namespace, partition), and are the defaults 8 partitions per observer and 16 requests per target authority acceptable, configurable per target? **Recommendation:** yes; revisit when [decision 0004](0004-ordered-lanes-observer-delivery.md) is decided.

### Q5. Delivery identity across replays

Must a replay of a range carry a different `webhook-id` than the original delivery (adding a replay epoch to the hash), and is the per-event deduplication key (observer, namespace, sequence number) the documented receiver guidance? **Recommendation:** yes to both. This changes the formula written in [#5117](https://github.com/Cratis/Chronicle/issues/5117).

### Q6. Partial acknowledgement and payload limits

Adopt `200` with a JSON result for partial acknowledgement, `413` halving instead of pausing the observer, and batch limits of 100 events and 4 MiB? **Recommendation:** yes. Note that `413` currently pauses the observer.

### Q7. Retry and park policy for HTTP reactors and reducers

Do the webhook defaults shipped in [#5108](https://github.com/Cratis/Chronicle/issues/5108) apply unchanged to HTTP reactors and reducers, including quarantine on by default? **Recommendation:** yes, through the same `Webhooks:Retry` section.

### Q8. Mandatory signing and default algorithm

Is signing mandatory for envelope deliveries (unless `AllowUnauthenticated` is set), with Ed25519 `v1a` as the default and HMAC `v1` as the alternative? What happens if the chosen .NET verifier lacks `v1a`? **Recommendation:** mandatory, Ed25519 by default; if the library lacks `v1a`, the receiver uses the kernel's own verification code for `v1a` and the library for `v1`.

### Q9. Signing key scope and rotation

One key per event store or one per target? How long is the rotation overlap, and how does a receiver obtain the public key (registration response, kernel endpoint, CLI)? **Recommendation:** per event store, rotated through the CLI, keys stored encrypted with `IEncryption`, the public key returned by registration and by a kernel endpoint. Settled here only if Q1 keeps key custody in this record.

### Q10. Transport authentication alongside signatures

Do Basic, Bearer, OAuth, managed identity and static headers remain available for envelope deliveries, combined with the signature? Should epic decision 11 be revised now that managed identity exists? **Recommendation:** keep them all, combined with signing; record that managed identity is supported and that SigV4 remains deferred.

### Q11. Replay boundaries

Awaited `replay-started` and `replay-ended` per observer replay: are per-partition boundaries (for partition replays) also part of v1? What does the kernel do when `replay-ended` keeps failing? **Recommendation:** observer-level boundaries in v1; partition replays carry the `Replay` flag and the replay epoch only; a failing `replay-ended` follows the normal retry and park policy without blocking live delivery.

### Q12. Catch-up and redaction semantics

Is catch-up after downtime delivered without boundaries (not a replay), and is a redaction delivered as an ordinary event of the redaction type? Whether every catch-up path marks events `Initial` is unverified. **Recommendation:** yes to both, with a specification that pins the observation flag on each path.

### Q13. Deadline and platform timeout

Is `Cratis-Delivery-Deadline` the header name, and is "the platform request timeout must be at most the delivery timeout" a documented requirement or something the kernel checks at registration? **Recommendation:** document it in v1; check at registration later if adapters can report their platform limit.

## Unknowns

- Platform limits (Azure Functions HTTP trigger limit, Lambda Function URL timeout and payload, Cloud Run timeout) are taken from vendor documentation in [#5114](https://github.com/Cratis/Chronicle/issues/5114) and were not measured.
- The license and `v1a` support of the `StandardWebhooks` .NET package named in [#5117](https://github.com/Cratis/Chronicle/issues/5117) are unverified.
- Behavior with several receiver instances during a replay, and live deliveries interleaved with a replay, were not measured.
- Whether the observation flag is `Initial` on every catch-up path is not verified from code.
