---
id: 0020-kernel-http-delivery-contract
title: Deliver observer events to HTTP receivers through one versioned envelope, ordered per partition, identified by a deterministic delivery id, signed with Standard Webhooks, and bracketed by awaited replay boundaries
status: accepted
stage: none
class: contract
reversibility: costly
proposed: 2026-10-25
decided: 2026-10-25
decider: Einar
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

Accepted on 2026-10-25 by Einar ([verdict](https://github.com/Cratis/Chronicle/pull/5398#issuecomment-6394945714)). The verdict decides four points: Ed25519 by default with HMAC opt-in and a key id header, a reserved fencing field, an envelope of per-partition batches, and one canonical per-event dedup key. Amended on 2026-10-30 ([amendment](#amendment-http-delivery-v1-is-live-only)): HTTP delivery v1 is live-only, which settles [Q5](#q5-delivery-identity-across-replays) for v1. Every other question keeps its recommended answer. See [Questions and the decider's verdict](#questions-and-the-deciders-verdict).

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
| Timeouts | Webhook subscribers are not retried immediately after a timeout; the job yields and recovery belongs to the observer (`HandleEventsForPartition.cs` L244-L250). The abandoned-delivery hold and delivery fences apply to client-owned subscribers only; webhooks are kernel-owned and get no hold, and a fence expires by time rather than proving the remote call finished. | `Source/Kernel/Core/Observation/Jobs/HandleEventsForPartition.cs` L244-L250, L507-L512; `Source/Kernel/Core/Observation/DeliveryFences.cs` L52-L57 |
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

### What a receiver cannot do today

A function host cannot hold a gRPC stream, so HTTP is its only delivery path. On the current tree a receiver cannot:

- **Verify the sender.** A Lambda Function URL with `AuthType NONE` or an anonymous Azure Function is public, and the kernel sends no signature (`Source/Kernel/Core/Observation/Webhooks/WebhookHttpClientFactory.cs` L35-L71).
- **Tell a retry from a replay.** There is no delivery id, and the per-event `Replay` flag is the only replay signal.
- **Reset before a replay.** Webhook observers get no replay boundary (`Source/Kernel/Core/Observation/ReactorReplayHandler.cs` L26, L90).
- **Accept part of a batch.** Any `2xx` acknowledges all of it and the body is not read (`WebhookMediator.cs` L59-L65).
- **Survive a platform size limit.** A `413` pauses the whole observer, although platforms answer `413` themselves (Vercel `FUNCTION_PAYLOAD_TOO_LARGE` above 4.5 MB).
- **Stop in time.** No deadline is sent, and the kernel's fences do not cover webhooks.

Each receiver built against today's body binds to an internal record, so every one of these fixes becomes a breaking change later. That is why the contract is fixed before [#5119](https://github.com/Cratis/Chronicle/issues/5119), [#5120](https://github.com/Cratis/Chronicle/issues/5120) and [#5121](https://github.com/Cratis/Chronicle/issues/5121) ship.

### Serverless architecture fit

Serverless platforms deliver events in three ways: a platform trigger polls a broker ([Lambda and Kinesis](https://docs.aws.amazon.com/lambda/latest/dg/with-kinesis.html), [Azure Event Hubs](https://learn.microsoft.com/en-us/azure/azure-functions/functions-reliable-event-processing)); a service pushes to an HTTPS endpoint ([Pub/Sub push](https://cloud.google.com/pubsub/docs/push), [Event Grid](https://learn.microsoft.com/en-us/azure/event-grid/delivery-and-retry)); or a consumer pulls. This record is the push model, with the kernel as the broker. Broker sinks ([#5134](https://github.com/Cratis/Chronicle/issues/5134), [#5135](https://github.com/Cratis/Chronicle/issues/5135)) cover the first model and pull mode ([#5125](https://github.com/Cratis/Chronicle/issues/5125)) the third.

The contract follows established practice:

- One request in flight per partition is the rule Pub/Sub push applies per ordering key ([ordering](https://cloud.google.com/pubsub/docs/ordering)).
- Partial acknowledgement up to `lastSuccessfulObservation` is the Kinesis checkpoint rule of `ReportBatchItemFailures`, and Lambda also reports a partial batch as a successful invocation ([batch failures](https://docs.aws.amazon.com/lambda/latest/dg/services-kinesis-batchfailurereporting.html)).
- `413` halving is `BisectBatchOnFunctionError` narrowed to size errors.

Three deviations are deliberate:

- Acknowledgement means "processed". A receiver cannot answer `202` and queue the work without losing per-partition order. Teams that want a queue use a broker sink.
- Vendor authentication is deferred. Lambda function URLs (`AWS_IAM`), private Cloud Run services (Google ID token) and Azure function keys are platform mechanisms the kernel does not speak in `v1`. Receivers are reachable endpoints that verify the signature.
- There is no external dead-letter destination. Parked partitions stay in the kernel and alerts come from kernel metrics.

The kernel is not serverless. On an empty store it opened its port after 15.2 s and used 515 MB ([#5104](https://github.com/Cratis/Chronicle/issues/5104) measurements). The fit is "functions for the app tier, an always-on kernel". Platform cold starts were not measured on real platforms ([#5114](https://github.com/Cratis/Chronicle/issues/5114)).

## Decision

The kernel delivers events to Cratis HTTP receivers as `POST` requests with the content type `application/vnd.cratis.chronicle.delivery+json; v=1`, whose body is a versioned envelope of kind `events`, `replay-started`, `replay-ended` or `ping`, carrying the observer, event store, namespace, event sequence and a list of per-partition batches. Each batch carries its partition, its events in a pinned public shape and a reserved, monotonically increasing fencing token; the response carries one result per batch. Version 1 sends one partition per request, so sending several later is not a breaking change. Delivery is at least once and ordered per (observer, namespace, partition), with at most one request in flight per partition from the kernel's side (the kernel bounds its own dispatch and wait, not how long a receiver keeps executing an abandoned request), and with per-target concurrency caps that include replay. Each request carries a deterministic `webhook-id` that is identical across retries of the same request, together with `webhook-timestamp`, a `webhook-signature` per the Standard Webhooks specification (Ed25519 `v1a` by default, HMAC `v1` as an opt-in alternative), a `Cratis-Key-Id` header naming the signing key so keys can rotate, a deadline header and `traceparent`. Each event has one canonical, deterministic dedup key, shared with the broker sinks and separate from the partition, which only orders. How a replay interacts with deduplication is not decided (see [Q5](#q5-delivery-identity-across-replays)). Retry, deferral, park and quarantine follow the policy shipped in [#5108](https://github.com/Cratis/Chronicle/issues/5108), extended with partial acknowledgement and payload limits. A replay is bracketed by `replay-started` and `replay-ended`, and the kernel dispatches no replayed partition before the receiver has acknowledged `replay-started`. Within this major release generic webhooks keep today's body until they opt in.

### Envelope

- Media type `application/vnd.cratis.chronicle.delivery+json; v=1`. A receiver that does not support the version answers `415`, which pauses the observer as a configuration error (today's classification).
- Body fields: `kind`, `observerType`, `observerId`, `eventStore`, `namespace`, `eventSequenceId`, `deliveryId` (equal to `webhook-id`) and `batches[]`. Each batch has `partition`, `events[]`, `fencingToken` and, for reducers, `initialState` (from [#5116](https://github.com/Cratis/Chronicle/issues/5116)). Version 1 sends exactly one batch per `events` request; a receiver processes every batch it is given and answers one result per batch. The kernel sends more than one batch only to a target that declared support for it ([Q16](#q16-unknown-data-kinds)), so raising throughput later is not a breaking change. A signed `replayEpoch` (the replay-operation identifier, see [Replay](#replay-catch-up-and-redaction)) is present on `replay-started`, `replay-ended` and on `events` deliveries that belong to a replay; it is absent (null) on ordinary deliveries and `ping`.
- Events use a pinned public shape defined in the contracts, not the kernel's `Concepts.Events.AppendedEvent`. Each event carries its context (including sequence number, event type with the delivered generation, observation state, correlation and causation) and its content. Internal storage fields (`OriginalContent`, `GenerationalContent`, `GenerationalHashes`) are not part of v1.
- Unknown fields are ignored by receivers; adding a field is not a version change. Removing or renaming a field, or changing a semantic, is.
- Receivers ignore an unknown control kind and answer `2xx`, so a new control kind (for example per-partition replay boundaries) is an additive change, not a new version. The kernel sends a new data kind (one that carries events) only to a target that declared support for it, in its target configuration or its answer to `ping`; a receiver that gets an unknown data kind answers `415` ([Q16](#q16-unknown-data-kinds)).
- `fencingToken` is reserved in v1. It is null until the partition-claim epoch of [decision 0018](0018-partition-ownership-protocol.md) is implemented. Once populated it is strictly increasing per observer key and partition across retries, timeouts, park and resume, kernel restarts and failover, and is never reused; receivers that commit effects to their own store can reject a lower token ([Q14](#q14-receiver-enforced-fencing)).

Why unknown data kinds are not ignored: this is the one part of the envelope that cannot be fixed after receivers exist. An old receiver that ignored an unknown kind and answered `2xx` would acknowledge events it never read.

### Ordering and concurrency

- One request in flight per (observer, namespace, partition). The kernel never starts a retry while its previous request for the partition is awaited. After a timeout the kernel does not retry immediately: the job yields and the observer owns recovery after the backoff delay (today's webhook behavior). That bounds the kernel's own dispatch and waiting only. A receiver that ignores cancellation can keep executing an abandoned request beyond the deadline plus backoff, and the kernel's abandoned-delivery hold and delivery fences do not cover webhooks today. Remote overlap is therefore unbounded unless the receiver enforces execution ownership itself with the reserved fencing token ([Q14](#q14-receiver-enforced-fencing)); receivers must treat overlap as possible.
- Partitions run in parallel up to a per-observer limit, and a per-target-authority limit applies across live delivery, catch-up, retries and replay. Proposed defaults are 8 and 16 ([#5116](https://github.com/Cratis/Chronicle/issues/5116)).

**Notes (decided by Q15 as recommended):** live delivery is serial per observer today. `Observers.MaxConcurrentPartitions` states that "live delivery never processes partitions of one observer concurrently" (`Source/Kernel/Core/Configuration/Observers.cs` L17-L18), and the appended-events queue awaits every observer for one partition before the next (`Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs` L471-L506). Parallel live delivery is therefore new kernel work, not a setting ([Q15](#q15-live-path-concurrency)). Replay and catch-up concurrency is a different limit: it follows the kernel's CPU count, `Math.Max(1, Environment.ProcessorCount - 1)` unless `MaxParallelSteps` is set (`Source/Kernel/Core/Configuration/Jobs.cs` L69), so the proposed per-observer cap of 8 is not reached on a small kernel unless the two are decoupled.
- No order is promised across partitions or across observers.
- Ordering and identity are scoped to the full observer key (observer id, event store, namespace and event sequence) plus partition, never to a subset of it.
- An HTTP delivery is a writer in the sense of [decision 0018](0018-partition-ownership-protocol.md): it delivers only while it holds the partition claim. If [decision 0004](0004-ordered-lanes-observer-delivery.md) (proposed) is accepted, its sequencing key replaces the partition as the ordering unit without changing the envelope.

### Idempotency, acknowledgement and retries

- `webhook-id` is a deterministic hash of (event store, namespace, observer id, event sequence id, kind and, for each batch, partition, first sequence number, last sequence number). The same request on retry carries the same id. The hash has no replay component in v1, because v1 delivers no replays ([amendment](#amendment-http-delivery-v1-is-live-only)).
- Each event has one canonical, deterministic dedup key (event store, namespace, observer id, event sequence id, sequence number) with one fixed encoding, shared with the broker sinks and separate from the partition or ordering key ([Q21](#q21-canonical-per-event-identity)). Receivers deduplicate side effects per event on it, not per request, because a partial acknowledgement or a halved batch changes batch boundaries. Reactor side-effect appends are keyed `{observerId}:{sourceSequenceNumber}` once the client exposes keyed appends (epic decision 1; [decision 0012](0012-keyed-appends-travel-on-their-own-rpcs.md)).
- Replay receipts do not apply in v1 ([amendment](#amendment-http-delivery-v1-is-live-only)). A persistent side-effect receipt is keyed on the per-event dedup key. A rebuild receipt scoped to a replay epoch is for a later record that enables replay over HTTP.
- `2xx` with an empty body or `204` acknowledges the whole batch. `200` with a JSON result `{results: [{partition, state, lastSuccessfulObservation, exceptionMessages, exceptionStackTrace, readModelState}]}` has one entry per batch of the request and acknowledges each partition up to its `lastSuccessfulObservation`, failing from the next event. v1 requests have one batch, so one entry. The kernel ignores unknown fields in that result. The kernel reads no response body today (`WebhookMediator.cs` L59-L65), so this is a requirement on the new result parser.
- `413` halves the batch instead of pausing the observer; a single event over the size limit parks the partition with reason `PayloadTooLarge`. Batches are bounded by an event count and a byte size (proposed 100 events and 4 MiB).
- `429`, and `503` with `Retry-After`, defer the partition that got the answer today (`WebhookMediator.cs` L123-L127). Platforms throttle per function, not per partition: a Lambda function URL answers `429` when the function exceeds its reserved concurrency ([function URLs](https://docs.aws.amazon.com/lambda/latest/dg/urls-configuration.html)), and the CloudEvents webhook specification requires the sender to "refrain from sending further requests until the indicated time" ([spec](https://github.com/cloudevents/spec/blob/main/cloudevents/http-webhook.md)). Decided by [Q17](#q17-target-wide-back-off) as recommended: such an answer makes the whole target authority back off, not one partition.
- All other classifications, the retry schedule, the 24-hour budget, park and quarantine stay as shipped in [#5108](https://github.com/Cratis/Chronicle/issues/5108) and documented in `Documentation/webhooks/index.mdx`.
- A `Cratis-Delivery-Deadline` header (UTC) tells the receiver when the kernel abandons the request. The function's execution timeout (Azure Functions `functionTimeout`, the Lambda function timeout) must be at most the delivery timeout. An HTTP front's timeout is not enough: the Azure Functions load balancer answers `502` after 230 seconds while the function keeps running, and an API Gateway HTTP API answers after 30 seconds while the Lambda runs on. The kernel classifies both as transient failures and retries while the first execution is still running.

### Authentication and signatures

- Every envelope delivery is signed per Standard Webhooks: `webhook-id`, `webhook-timestamp` (Unix seconds) and `webhook-signature` over `id.timestamp.body`. The default is Ed25519 (`v1a`), so a receiver holds only a public key; HMAC-SHA256 (`v1`) is an opt-in alternative for receivers that only support shared secrets. Every delivery carries the id of the signing key in the `Cratis-Key-Id` header, so a receiver picks the right key and keys can be rotated; several signatures can be sent at once for an overlap, and the header then lists every signing key id in signature order ([0021](0021-delivery-signing-key-custody.md)).
- An HMAC `v1` secret is unique per target, as the Standard Webhooks specification requires for symmetric keys; a shared secret would let one receiver forge deliveries to another. An Ed25519 key may be shared by the targets of one event store, because receivers hold only the public key. Receivers take the public key from configuration, never from the request.
- No published Standard Webhooks library verifies `v1a`, and .NET has no built-in Ed25519 ([dotnet/runtime#63174](https://github.com/dotnet/runtime/issues/63174)). The kernel signs and the .NET receiver verifies `v1a` with code owned by this repository on top of one named Ed25519 library. `v1` is the interoperable choice for receivers in other languages.
- Receivers verify the signature in constant time, reject timestamps outside a tolerance (proposed 5 minutes), and never follow redirects.
- The existing transport authentication modes stay available and combine with signing: Basic, Bearer, OAuth client credentials, managed identity and static headers (for example a platform function key). `None` without a signature requires an explicit `AllowUnauthenticated` setting and logs a startup warning ([#5117](https://github.com/Cratis/Chronicle/issues/5117)).
- Signing is opt-in for generic webhooks, which keep today's body.
- The existing target rules stay part of the contract: HTTPS outside development, cloud metadata addresses only through an explicit allow-list (`Source/Kernel/Core/Observation/Webhooks/Validation/WebhookTargetPolicy.cs` L76-L87), and no redirects (`Source/Kernel/Core/Setup/WebhookHttpClientExtensions.cs` L78).
- Key custody (scope, storage under the kernel's `IEncryption`, rotation, distribution of the public key) is decided in [decision 0021](0021-delivery-signing-key-custody.md) ([Q1](#q1-one-record-or-a-split), [Q9](#q9-signing-key-scope-and-rotation)).

### Replay, catch-up and redaction

- In v1 the kernel delivers no replay over HTTP ([amendment](#amendment-http-delivery-v1-is-live-only)). The bullets below on `replay-started`, `replay-ended` and the replay epoch describe the contract a later version sends; v1 sends none of it.
- Before the first replayed partition the kernel sends `replay-started` and waits for a `2xx`; a non-`2xx` backs off and retries instead of dispatching. After the last partition it sends `replay-ended`. Both carry the replay epoch.
- The replay epoch is a replay-operation identifier persisted with the replay job or context when the replay is scheduled (no replay counter exists on the observer state today). It is reused unchanged by retries and when the replay resumes, never reallocated. A whole-observer replay and a partition replay have separate lifecycles and separate epochs; a partition's delivery reads the epoch of the replay operation it belongs to, not an observer-wide mutable value, so another partition starting a replay does not change an unrelated partition's `webhook-id`. Whether concurrent replays of the same partition are possible is unknown and must be settled before implementation.
- An observer-level `replay-ended` is a control delivery parked on its own; its failure follows the retry and park policy but must not quarantine partitions or stop live delivery.
- Replayed events keep the `Replay` observation flag. Catch-up after downtime is not a replay: it is delivered as ordinary `events` deliveries with no boundary.
- A redaction is delivered as an ordinary event of the redaction type, as today. Revised events are delivered from the revision, migrated to the pinned generation, per [decision 0002](0002-event-generation-preservation-and-delivery.md), as live events only; v1 does not re-push earlier events. Content is sent in plaintext after compliance release over HTTPS (epic decision 6); encrypted or redacted delivery is [#5132](https://github.com/Cratis/Chronicle/issues/5132).

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
- **A deterministic hash including a replay epoch.** Needed only when replay is delivered. Deferred with replay ([amendment](#amendment-http-delivery-v1-is-live-only)).

### Acknowledgement

- **Whole-batch acknowledgement only (today).** No receiver work, but a failure on the last event redelivers every earlier event, which the spike measured as nine duplicates for three effects.
- **Partial acknowledgement through a JSON result.** Chosen. It mirrors `ObserverSubscriberResult` on the stream path and lets the kernel advance to the last successful event. The receiver must still deduplicate, because a lost response looks like a failure.

### Signatures and authentication

- **Transport authentication only (today).** Works for webhooks behind an authenticated gateway. A function exposed with no platform authentication (for example a Lambda Function URL with `AuthType NONE`) then has nothing to verify, and a bearer secret must be distributed to every function. Rejected as the default for HTTP reactors and reducers.
- **HMAC `v1` only.** Widest receiver library support, including Svix verifiers. Every receiver holds a secret that can forge deliveries. Kept as an alternative, not the default.
- **Ed25519 `v1a` by default.** Chosen. A leaked function configuration cannot forge deliveries. The cost is key lifecycle in the kernel, and `v1a` verification the project must own, because the .NET library named by [#5117](https://github.com/Cratis/Chronicle/issues/5117) verifies `v1` only (see [Q8](#q8-mandatory-signing-and-default-algorithm)).
- **HTTP Message Signatures (RFC 9421) or JWS.** Standards with broader scope, but less receiver tooling in the webhook ecosystem and no ready verifier in the platforms targeted. Rejected.
- **Mutual TLS.** Strong, but not available on the managed HTTP fronts of Azure Functions and Lambda Function URLs without extra infrastructure. Rejected as a default.
- **Cloud IAM (AWS SigV4, Entra ID).** Native to each platform but different on each. Deferred by epic decision 11, except that the managed identity mode already exists for webhooks.

### Replay signaling

- **The per-event `Replay` flag only (today).** No new requests, but a receiver cannot tell when a replay starts (to reset state) or ends (to resume side effects), and over HTTP the start and the first partition can land on different instances.
- **Fire-and-forget begin and end, as on the stream.** Already shown to race on the stream path ([#5116](https://github.com/Cratis/Chronicle/issues/5116)); over HTTP the risk is larger. Rejected.
- **Awaited `replay-started` and `replay-ended`.** Chosen. One extra request per replay; partitions wait on an acknowledgement.
- **Per-partition replay boundaries as well.** The stream handler has `BeginReplayPartitionFor` and `EndReplayPartitionFor` (`ReactorReplayHandler.cs` L57-L88). Not in v1 ([Q11](#q11-replay-boundaries)).

### Partial failure status

- **`200` with a result (chosen).** Matches `ObserverSubscriberResult` on the stream path. The cost is that a partial failure is a `200`, so platform dashboards and alerts count it as a success.
- **Non-`2xx` with the same result.** Platform metrics see a failure as a failure. Some gateways replace error bodies, so the kernel would treat a missing body as "nothing acknowledged", which is safe but adds duplicates. Not chosen, to keep one meaning across stream and HTTP.
- **A per-event status array.** Finer than the kernel can use, since the observer advances to one last-successful position. Rejected.

### Deadline format

- **Absolute UTC time (chosen).** Easy to log and compare. Depends on clock sync, which signature timestamp checks already assume, but at a 5-minute tolerance rather than seconds.
- **Relative remaining time, like gRPC's `grpc-timeout`.** Immune to clock skew; loses only transit time. A relative header can be added later without a version change.

### Signing key scope

- **Per kernel.** One key; a compromise affects every event store. Rejected.
- **Per event store (chosen for `v1a`).** One public key per store to distribute. Allowed by the specification for asymmetric keys.
- **Per target (required for `v1`).** Smallest blast radius; many secrets to rotate.

### Receiver-enforced fencing

- **Document overlap only (the recommendation, not chosen).** No kernel work. Overlap after a timeout stays unbounded when the function's execution timeout exceeds the delivery timeout or the function sits behind an HTTP front.
- **A fencing token in the envelope (chosen as a reserved field in v1).** Receivers that commit effects to their own store reject a commit whose token is lower than the highest stored for the partition (the fencing pattern in [Kleppmann, "How to do distributed locking"](https://martin.kleppmann.com/2016/02/08/how-to-do-distributed-locking.html)). It must strictly increase per observer key and partition across retries, timeouts, park and resume, kernel restarts and failover, and must never be reused. A retry count does not qualify: the failed-partition attempt history is removed when a partition recovers (`Source/Kernel/Concepts/Observation/FailedPartitions.cs` L140), and the observer's ownership epoch is in memory only (`Source/Kernel/Core/Observation/Observer.cs` L136). The partition-claim epoch of [decision 0018](0018-partition-ownership-protocol.md) has the required properties but is not implemented yet. The token does not protect external effects such as a sent email; those still need the per-event key.
- **Platform timeouts only.** Require execution timeout ≤ delivery timeout. Cheap but not enforceable by the kernel, and not sufficient behind HTTP fronts.

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
2. Envelope v1, deadline header and `traceparent`, opt-in per target. Within this major release the legacy body remains the default; a later major may switch the default ([Q3](#q3-generic-webhooks)).
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
- A specification shows `webhook-id` identical across retries of one request; and `webhook-id` and the per-event dedup key do not collide across two event stores or two event sequences with the same observer id, namespace, partition and sequence numbers. Specifications for replay identity and replay receipts are written once [Q5](#q5-delivery-identity-across-replays) is decided.
- A specification pins the per-event dedup key and its encoding, shared by HTTP deliveries and the broker sinks, and shows the batch list, one result per batch and a null `fencingToken` on a v1 request.
- Fault-injection specifications show never more than one awaited request in flight per partition from the kernel (a request abandoned after its deadline is outside this guarantee and the specification asserts only the kernel's dispatch and wait behavior), and in-flight requests to one target authority never above the cap during a replay with several partitions.
- Specifications cover `204`, `200` with a partial result, `413` halving, an oversized single event parking with `PayloadTooLarge`, and the existing `429`, `410` and `4xx` classifications unchanged.
- A specification shows no replayed partition dispatched before `replay-started` is acknowledged, and `replay-ended` sent after the last partition. This applies once replay is delivered over HTTP; for v1 a specification instead shows that no replay reaches an HTTP target ([amendment](#amendment-http-delivery-v1-is-live-only)).
- An interoperability specification verifies an HMAC `v1` delivery with a published Standard Webhooks verifier and an Ed25519 `v1a` delivery with the receiver's verifier; a modified body, an expired timestamp and a wrong key are rejected.
- Within this major release, existing generic webhooks without opt-in receive the same body and headers as before (regression specification).
- `Documentation/webhooks/` describes the receiver contract, the guarantee wording and the deduplication guidance.

**Verify by:** the kernel specification suites under `Source/Kernel/Core.Specs` and the client receiver suites run in the `dotnet-build` workflow, the interoperability specification passing against the named verifier libraries, and a local run of one HTTP reactor (as in [#5114](https://github.com/Cratis/Chronicle/issues/5114)) showing the envelope and signature headers on the wire (and the replay handshake once replay is delivered over HTTP).

## Consequences

**Easier:** receivers in any language can be written against a documented, versioned contract; the .NET receiver core and the platform adapters share one verification and dispatch path; operators can correlate a request with an observer, partition and replay from headers alone; broker sinks can reuse the same identity fields.

**Harder:** the project owns a public wire contract and its compatibility; the kernel gains signing key lifecycle; replays wait on a receiver acknowledgement, so an unreachable receiver delays a replay instead of letting it race; partial acknowledgement adds a response parser to the delivery path. The kernel and the .NET receiver take a third-party Ed25519 dependency, and `v1a` verification is code this repository owns. A partial failure answered with `200` looks like a success in platform dashboards. The result's `exceptionStackTrace` carries receiver stack traces across the network into kernel storage.

**Foreclosed:** sending the kernel's internal event record over HTTP to Cratis receivers; fire-and-forget replay notification over HTTP; unsigned envelope deliveries unless an operator explicitly allows them.

## Performance and capacity model

Live delivery today runs one request at a time per observer: the appended-events queue awaits every observer for one partition before the next (`Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs` L471-L506), the webhook subscriber awaits the HTTP call (`Source/Kernel/Core/Observation/Webhooks/WebhookObserverSubscriber.cs` L76), and `Observers.MaxConcurrentPartitions` says so (`Source/Kernel/Core/Configuration/Observers.cs` L17-L18). A slow HTTP receiver therefore also delays every other observer on its queue until the queue spills to catch-up (`QueueBoundedCapacity`, `Source/Kernel/Core/Configuration/Events.cs`).

Catch-up and replay run one job step per partition, capped at the smaller of `Observers.MaxConcurrentPartitions` (32) and `ProcessorCount - 1` (`Source/Kernel/Core/Configuration/Jobs.cs` L69). On a 2-vCPU kernel that is one request in flight.

With request latency `L`, events per request `b` and effective concurrency `c`, throughput is about `c × b / L`. Replay time is the larger of `N / (c × b) × L` and the longest partition's own sequential time.

| Scenario (L = 50 ms live, 300 ms per 100-event batch; assumptions, not measurements) | Today | This decision (caps 8/16) |
| --- | --- | --- |
| Live, distinct entities, 1 event each | about 18 events/s per observer | about 160 per observer, 320 per target |
| Replay, 10 M events over 100,000 entities, 2-vCPU kernel | about 8.3 h (`c` = 1) | about 62 min (`c` = 8), 31 min (`c` = 16), only with the cap decoupled from CPU count |
| Replay, one entity with 1 M events | about 50 min, whatever the cap | same |

Function cost is not the constraint. 10 M single-event deliveries at 512 MB and 50 ms cost about $5 on Lambda arm64 ([pricing](https://aws.amazon.com/lambda/pricing/)) and about $10.50 on Azure Flex Consumption on demand ([retail prices](https://prices.azure.com/api/retail/prices?$filter=serviceName%20eq%20%27Functions%27%20and%20armRegionName%20eq%20%27eastus%27)). Batching replays by 100 cuts that by about 25 times. The always-on kernel, its database and the observer-state writes per delivery are the larger costs; neither was measured.

## Relation to webhook observers

Webhook observers and HTTP reactors share the transport (`WebhookMediator.cs`), the observer machinery and the live-path limits above. Fixing live concurrency for one fixes the other. The per-target cap counts both kinds of delivery to the same authority, because they draw on the same function's concurrency.

Within this major, webhooks keep today's body ([Q3](#q3-generic-webhooks)). At the next major, one HTTP target concept with a `format` setting is proposed: `chronicle-v1` (default), `legacy` (deprecated), and later `cloudevents`. That leaves one delivery path, one classification table and one place for capability negotiation (see the open point on unknown kinds under [Envelope](#envelope)).

## Amendment: HTTP delivery v1 is live-only

Decided on 2026-10-30 by woksin (Q-146). HTTP delivery v1 delivers live events only. The kernel does not deliver replays or resets over HTTP.

**In v1, over HTTP:**

- Initial catch-up after the observer is first subscribed, catch-up after downtime and retries of a failed delivery are delivered. They are ordinary `events` deliveries with no boundary, and a retry carries the same `webhook-id`.
- An observer that serves HTTP is not replayable. Replaying it, rewinding a partition and any definition-change replay are not started for it; a request to do so is rejected or recorded as a recommendation, never delivered.
- The kernel sends no `replay-started` or `replay-ended` request. The kinds stay in the contract so that a later version can send them without a new contract version.
- Revised events and redactions are delivered only as the live events they are today. The kernel does not re-push earlier events when they are revised or redacted.

**Why.** Deduplication by `webhook-id` or by the per-event dedup key suppresses a replay, and a replay epoch in the id is not settled ([Q5](#q5-delivery-identity-across-replays)). A replay also races with live delivery and with an abandoned request that is still running, and the fencing token that would order them depends on the partition-claim epoch of [decision 0018](0018-partition-ownership-protocol.md), which is not enforced yet. Live delivery needs neither.

**Consequences.**

- `webhook-id` keeps the formula in [Idempotency, acknowledgement and retries](#idempotency-acknowledgement-and-retries) with no replay component. A later record that enables replay over HTTP decides the epoch, the receipts and the fencing check together, and changes `webhook-id` only for replay deliveries.
- The `Replay` observation flag and `replayEpoch` stay reserved in the envelope. A v1 receiver can ignore them.
- A reducer or reactor on HTTP cannot be rebuilt from the event log. A receiver that needs a rebuild uses a stream observer, or re-reads state from its own store.
- Webhook observers must honour the replayability setting before an HTTP reactor or reducer ships. Today they ignore it.

**Alternatives considered.**

- *Fencing first.* Land the partition-claim epoch of decision 0018, then ship replay with a receiver-checked fencing token. Correct, and the recommended path for replay, but it puts a hot-core kernel change in front of every serverless use that only needs live delivery. Deferred, not rejected.
- *Replay with known gaps.* Ship replay with the epoch in `webhook-id` and document that an abandoned request or a concurrent replay can overlap, and that revised and redacted events are not re-pushed. It gives rebuilds earlier, at the price of a contract whose `webhook-id` and receipt rules ([Q5](#q5-delivery-identity-across-replays), reversibility: no) are fixed before the overlap is understood. Rejected for v1.

### Receiver responsibilities

- Delivery is at least once. Make every effect idempotent, and commit the effect and its dedup receipt in one transaction. A receipt written before the effect loses the effect on a crash; one written after it repeats the effect.
- Deduplication prevents repeated effects. It does not make receiver state correct. After a timeout the kernel can retry while the first execution is still running, so two executions of the same event can overlap. Take a per-partition lock, or check the order of `sequenceNumber` in the same transaction.
- Deduplicate per event on the canonical key ([Q21](#q21-canonical-per-event-identity)), not per request: batch boundaries change on a partial acknowledgement or a halved batch.
- Redactions are not pushed. A redaction event reaches the receiver only as a live event. Data the receiver already stored is not corrected by the kernel, so removing or redacting it is the receiver's responsibility. Encrypted or redacted delivery is [#5132](https://github.com/Cratis/Chronicle/issues/5132). This record makes no compliance claim.
- Verify the signature on every request, take the event store and namespace from the signed body and not from the route, and take the public key from configuration. A proxy that strips identity headers does not authenticate the body; see [decision 0021](0021-delivery-signing-key-custody.md).

## Questions and the decider's verdict

Each question below keeps the recommendation it was put with. The verdict covers only the points marked in the table; every other question is decided as recommended. Where the verdict differs from a recommendation, the Decision section and the affected sections above follow the verdict.

| Question | Status | Note |
| --- | --- | --- |
| Q1 | Decided as recommended | Key custody splits into a follow-up record. Key custody is decided in decision 0021. |
| Q2 | Decided differently | Pinned public shape as recommended; the body carries a list of per-partition batches. |
| Q3, Q4 | Decided as recommended | |
| Q5 | Settled for v1 by the amendment | HTTP delivery v1 is live-only, so no replay epoch or replay receipt applies. The per-event dedup key is decided under Q21. |
| Q6 | Decided as recommended | The result carries one entry per batch. |
| Q7 | Decided as recommended | |
| Q8 | Decided by the verdict | Ed25519 by default, HMAC opt-in; mandatory signing and the verifier code as recommended. |
| Q9 | Decided as recommended | The key id travels in the `Cratis-Key-Id` header; custody details follow the Q1 split. |
| Q10, Q11, Q12, Q13 | Decided as recommended | The timeout conflict in Q13 stays under Unknowns. |
| Q14 | Decided differently | The fencing field is reserved in v1 instead of deferred to a follow-up record. |
| Q15, Q16, Q17 | Decided as recommended | |
| Q18 | Decided differently | The envelope is a list of per-partition batches with per-partition results; v1 sends one partition per request. |
| Q19, Q20 | Decided as recommended | |
| Q21 | Decided by the verdict | One canonical, deterministic per-event dedup key shared with the broker sinks, separate from the ordering key; the five-part key as recommended. |

### Q1. One record or a split

Should this record also settle signing key custody (scope, storage, rotation, public-key distribution), or should that move to its own record, with this one keeping only the wire signature scheme? **Recommendation:** split key custody into a follow-up record; keep the wire scheme here. Transport ownership stays with [#5115](https://github.com/Cratis/Chronicle/issues/5115) in either case.

### Q2. Envelope format and event shape

Native JSON with `application/vnd.cratis.chronicle.delivery+json; v=1`, and events in a pinned public shape rather than the kernel's internal record? Which internal fields (`OriginalContent`, `GenerationalContent`, `GenerationalHashes`, `Revisions`) are excluded from v1? **Recommendation:** yes, and exclude all four; the delivered generation and the revision are already resolved by the kernel.

### Q3. Generic webhooks

Existing webhooks keep today's body within this major release and move to envelope v1 by opt-in, as the Decision states. Should the default switch to envelope v1 at the next major release? **Recommendation:** decide that at the next major; if switched, keep the legacy body behind an explicit setting for one further major and retire it after. The cost of switching is a breaking change for every generic webhook receiver that did not opt in. This record guarantees only the within-major behavior.

### Q4. Ordering unit and concurrency defaults

Is the ordering unit (observer, namespace, partition), and are the defaults 8 partitions per observer and 16 requests per target authority acceptable, configurable per target? **Recommendation:** yes; revisit when [decision 0004](0004-ordered-lanes-observer-delivery.md) is decided.

### Q5. Delivery identity across replays

**Settled for v1:** HTTP delivery v1 is live-only ([amendment](#amendment-http-delivery-v1-is-live-only)). The question stays open for the record that enables replay over HTTP. The proposal put to the decider follows. Must a replay of a range carry a different `webhook-id` than the original delivery (adding a persisted replay-operation epoch to the hash), and is the per-event deduplication key (event store, namespace, observer id, event sequence id, sequence number) the documented receiver guidance, with once-only side-effect receipts kept across replays and rebuild receipts scoped to the replay epoch? **Recommendation:** yes to both. This changes the formula written in [#5117](https://github.com/Cratis/Chronicle/issues/5117).

### Q6. Partial acknowledgement and payload limits

Adopt `200` with a JSON result for partial acknowledgement, `413` halving instead of pausing the observer, and batch limits of 100 events and 4 MiB? **Recommendation:** yes. Note that `413` currently pauses the observer.

### Q7. Retry and park policy for HTTP reactors and reducers

Do the webhook defaults shipped in [#5108](https://github.com/Cratis/Chronicle/issues/5108) apply unchanged to HTTP reactors and reducers, including quarantine on by default? **Recommendation:** yes, through the same `Webhooks:Retry` section.

### Q8. Mandatory signing and default algorithm

Is signing mandatory for envelope deliveries (unless `AllowUnauthenticated` is set), with Ed25519 `v1a` as the default and HMAC `v1` as the opt-in alternative? (Decided: yes; the key id travels in a header.) What happens if the chosen .NET verifier lacks `v1a`? **Recommendation:** mandatory, Ed25519 by default; if the library lacks `v1a`, the receiver uses the kernel's own verification code for `v1a` and the library for `v1`.

**Risk:** no published Standard Webhooks library verifies `v1a`, and .NET has no built-in Ed25519 ([dotnet/runtime#63174](https://github.com/dotnet/runtime/issues/63174)), so the project would own that verification code plus one third-party Ed25519 dependency, whose runtime support on Azure Functions and Lambda is unverified.

### Q9. Signing key scope and rotation

One key per event store or one per target? How long is the rotation overlap, and how does a receiver obtain the public key (registration response, kernel endpoint, CLI)? **Recommendation:** per event store, rotated through the CLI, keys stored encrypted with `IEncryption`, the public key returned by registration and by a kernel endpoint. Settled here only if Q1 keeps key custody in this record.

**Note:** this recommendation holds for Ed25519 `v1a`. HMAC `v1` secrets are per target, not per store, because the Standard Webhooks specification requires a unique symmetric secret per receiver.

### Q10. Transport authentication alongside signatures

Do Basic, Bearer, OAuth, managed identity and static headers remain available for envelope deliveries, combined with the signature? Should epic decision 11 be revised now that managed identity exists? **Recommendation:** keep them all, combined with signing; record that managed identity is supported and that SigV4 remains deferred.

### Q11. Replay boundaries

Awaited `replay-started` and `replay-ended` per observer replay: are per-partition boundaries (for partition replays) also part of v1? What does the kernel do when `replay-ended` keeps failing? **Recommendation:** observer-level boundaries in v1; partition replays carry the `Replay` flag and the replay epoch only; a failing `replay-ended` follows the normal retry and park policy without blocking live delivery. **Amended:** v1 sends no replay boundary over HTTP.

### Q12. Catch-up and redaction semantics

Is catch-up after downtime delivered without boundaries (not a replay), and is a redaction delivered as an ordinary event of the redaction type? Whether every catch-up path marks events `Initial` is unverified. **Recommendation:** yes to both, with a specification that pins the observation flag on each path.

### Q13. Deadline and platform timeout

Is `Cratis-Delivery-Deadline` the header name, and is "the platform request timeout must be at most the delivery timeout" a documented requirement or something the kernel checks at registration? **Recommendation:** document it in v1; check at registration later if adapters can report their platform limit.

**Notes:** the function's execution timeout governs, not the HTTP front (a front can answer `502` or time out while the function keeps running), so the documented requirement is about execution timeout. The 60 second webhook request timeout default conflicts with the 30 seconds [#5118](https://github.com/Cratis/Chronicle/issues/5118) proposes; which one applies to HTTP reactors and reducers is undecided.

### Q14. Receiver-enforced fencing

Should the contract define a receiver-enforced execution or fencing token (for example a monotonic lease or attempt number the receiver checks before committing effects) so an abandoned request cannot land after a newer attempt? Without it, remote overlap after a timeout is unbounded. **Recommendation:** document the overlap as a receiver responsibility in v1 and decide a fencing token in a follow-up record once receivers exist. **Decided differently:** v1 reserves a monotonically increasing fencing field.

**Note:** a fencing token needs a durable, strictly increasing epoch. The partition-claim epoch of [decision 0018](0018-partition-ownership-protocol.md) fits but is not implemented yet, and a retry count or the in-memory observer epoch does not qualify. Adding an optional `fencingToken` field later is not a version change, so v1 can reserve it; once receivers compare it, weakening its monotonicity is.

### Reversibility of each answer

Adding a field or a `kind` is not a version change; changing a meaning is.

| Question | Changeable later without a new contract version |
| --- | --- |
| Q1 split | Yes |
| Q2 envelope fields | Adding yes; removing or renaming no |
| Q3 generic default | Yes, at a major release |
| Q4 ordering unit and caps | Caps yes; narrowing the ordering unit no |
| Q5 `webhook-id` and dedupe key | No; replay delivery is not in v1 |
| Q6 meaning of `200` with a result | No; limits and halving yes |
| Q7 retry and park | Yes |
| Q8 mandatory signing, algorithms | Adding algorithms yes; dropping signing or an algorithm no |
| Q9 key scope | Yes, if receivers take keys from configuration |
| Q10 transport authentication | Yes |
| Q11 observer-level boundaries | Adding kinds yes; dropping the await no |
| Q12 catch-up and redaction flags | No |
| Q13 deadline header | Name and format no; adding a relative header yes |
| Q14 fencing token | Adding the field yes; weakening its monotonicity no |
| Q15 live-path concurrency | Yes; no wire change |
| Q16 unknown data kinds | No once receivers ignore unknown kinds; adding capability declarations yes |
| Q17 target-wide back-off | Yes |
| Q18 multi-partition batches | Adding a kind yes, if Q16 holds |
| Q19 CloudEvents identity | Yes while text only |
| Q20 real-platform measurement | Yes |
| Q21 canonical event identity | No for the encoding once receivers and sinks store it |

## Later questions

These were raised after Q1 to Q14; their status is in the table above.

### Q15. Live-path concurrency

Live delivery to HTTP targets is serial per observer today and holds the queue loop. Does this decision require [#5116](https://github.com/Cratis/Chronicle/issues/5116) to move HTTP live delivery out of the queue loop, so partitions run in parallel under the per-observer and per-target caps, or does `v1` ship with serial live delivery and document about `1 / L` events per second per observer? The cap for HTTP targets should not be bound by `Jobs.MaxParallelSteps`, because HTTP delivery is I/O-bound. **Recommendation:** require it; make it an acceptance criterion of [#5116](https://github.com/Cratis/Chronicle/issues/5116), measured by a benchmark with a 50 ms receiver and an in-process projection on the same queue.

### Q16. Unknown data kinds

Replace "ignore any unknown kind and answer `2xx`" with: receivers ignore an unknown control kind; the kernel sends a new data kind (one that carries events) only to a target that declared support for it, in its target configuration or its answer to `ping`; a receiver that gets an unknown data kind answers `415`. **Recommendation:** yes. It is the only part of the envelope that cannot be fixed after receivers exist.

### Q17. Target-wide back-off

Make `429` and `503` with `Retry-After` defer every partition of the target authority, not only the partition that got the answer? Without `Retry-After` the kernel backs off exponentially, and these deferrals never count toward parking. **Recommendation:** yes.

### Q18. Multi-partition batches

Add a multi-partition kind in `v1`, like Kinesis and Event Hubs batches? **Recommendation:** no. With Q16 it can be added later for targets that opt in. **Decided differently:** the envelope is a list of per-partition batches with per-partition results from v1, and v1 sends one partition per request, so raising throughput later is not breaking. The head-of-line caveat applies when several are sent. It would also add head-of-line blocking on failure, which Lambda handles for SQS FIFO by stopping at the first failure ([SQS](https://docs.aws.amazon.com/lambda/latest/dg/services-sqs-errorhandling.html)).

### Q19. CloudEvents identity

Document now how a future CloudEvents format maps `id`, `source`, `subject` and `type` to the dedupe key (Q21), event store, namespace, sequence, partition and event type, so receivers never hold two dedupe keys? **Recommendation:** yes, text only.

### Q20. Real-platform measurement

Accept the decision before one paid cold-start and throttle run on Lambda, Azure Flex and Cloud Run? **Recommendation:** accept the contract; do not call serverless delivery supported until that run meets first delivery p50 under 2 s and p99 under 5 s.

### Q21. Canonical per-event identity

Define one key per event: (event store, namespace, observer id, event sequence id, sequence number), with one fixed encoding. HTTP receivers deduplicate side effects on it (see Idempotency above), broker sinks use it as `MessageId` (Service Bus, [#5134](https://github.com/Cratis/Chronicle/issues/5134)) and `MessageDeduplicationId` (SQS FIFO, [#5135](https://github.com/Cratis/Chronicle/issues/5135)), and keyed side-effect appends relate to it (they are keyed `{observerId}:{sourceSequenceNumber}` today, within one store). The two sink issues do not match it:

- [#5134](https://github.com/Cratis/Chronicle/issues/5134) derives `MessageId` from event store, namespace, observer and event sequence number. It omits the event sequence id.
- [#5135](https://github.com/Cratis/Chronicle/issues/5135) contradicts itself. Its summary says `MessageDeduplicationId` is the event sequence number alone; its publishing section says a stable id from event store, namespace, observer and event sequence number. Neither includes the event sequence id.

Without the event sequence id, two event sequences of one store with the same sequence number collide. **Recommendation:** pin the five-part key and its encoding in this record, and correct both issues to use it.

## Unknowns

- Platform limits (Azure Functions HTTP trigger limit, Lambda Function URL timeout and payload, Cloud Run timeout) are taken from vendor documentation in [#5114](https://github.com/Cratis/Chronicle/issues/5114) and were not measured.
- Resolved: the `StandardWebhooks` .NET package named in [#5117](https://github.com/Cratis/Chronicle/issues/5117) is MIT-licensed and verifies `v1` only (`codefactors/StandardWebhooks`, `src/StandardWebhooks/StandardWebhook.cs` L130). None of the official Standard Webhooks libraries verifies `v1a`.
- Which Ed25519 library the kernel and receiver use, and whether it runs on the Azure Functions isolated worker and on Lambda with SnapStart (including any native binaries), is not verified.
- Which timeout governs an HTTP reactor or reducer is not settled: the webhook request timeout defaults to 60 seconds (`Source/Kernel/Core/Configuration/Webhooks.cs` L67) while [#5118](https://github.com/Cratis/Chronicle/issues/5118) proposes `Serverless:Delivery:Timeout` of 30 seconds.
- RFC 9110 allows a `413` with `Retry-After` for a temporary condition; halving ignores it. Whether to honor `Retry-After` on `413` is not decided.
- For the record that enables replay: behavior with several receiver instances during a replay, and live deliveries interleaved with a replay, were not measured.
- For the record that enables replay: whether concurrent replays of one partition (whole-observer and partition replay) can overlap, and how a replay epoch relates to them, is not verified.
- Whether the observation flag is `Initial` on every catch-up path is not verified from code.
