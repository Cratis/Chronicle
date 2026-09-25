---
title: Where the clients differ
description: Differences in appending, concurrency, PII, reactor delivery, and seeding across Chronicle's .NET, Kotlin, Java, TypeScript, and Elixir clients.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

One kernel contract does not make the client APIs interchangeable. This reference covers appending and concurrency, compliance, reactor delivery, and event seeding. It does not claim parity for other features.

The comparisons below describe the client source inspected on September 23, 2026. Java uses Java-callable interfaces, blocking facades, and bridges that delegate to the Kotlin implementation. Source links identify the implementation behind each comparison. These are source-level comparisons, not cross-client runtime conformance results.

## Appending and concurrency

### Default concurrency checks

| Client | Append without an explicit concurrency scope |
| --- | --- |
| .NET | The default `OptimisticConcurrencyStrategy` reads the event source's tail within the selected stream scope and sends an expected sequence number. `ConcurrencyOptions.DefaultStrategy` can change this behavior. |
| Kotlin and Java | Use `ConcurrencyScope.none`; no optimistic concurrency check is requested. |
| TypeScript | Sends an unset expected sequence number; no optimistic concurrency check is requested. |
| Elixir | Sends no concurrency scope. |

The .NET default detects an intervening append between its tail read and write. It does **not** protect an earlier application read automatically. When no event matches the scope, the default leaves the first append unchecked; `CheckFirstAppendIntoAScope` enables that check and defaults to `false`.

Choose an explicit scope when correctness depends on the state you previously read. Do not infer the application's concurrency guarantee from a successful plain append.

Sources: [.NET strategy](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/EventSequences/Concurrency/OptimisticConcurrencyStrategy.cs), [.NET defaults](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/EventSequences/Concurrency/ConcurrencyOptions.cs), [JVM append](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/eventSequences/EventSequence.kt), [TypeScript append](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/eventSequences/EventSequence.ts), [Elixir append](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/event_sequences/event_log.ex).

### Expecting no matching event

.NET exposes `ConcurrencyScopeBuilder.ExpectingNoMatchingEvent`; the JVM builder exposes `withExpectsNoMatchingEvent()`, which its Java bridge documents as directly callable from Java. Both send the kernel's explicit no-matching-event condition.

The TypeScript and Elixir client scope converters do not send that condition. An unset expected sequence number is **not** a substitute: it disables the sequence-number check rather than asserting that no event exists.

Sources: [.NET conversion](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/EventSequences/Concurrency/ConcurrencyScopeConverters.cs), [JVM builder](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/eventSequences/concurrency/ConcurrencyScopeBuilder.kt), [kernel validation](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/EventSequences/Concurrency/ConcurrencyValidator.cs), and the TypeScript and Elixir append implementations linked above.

### Results and rejections

| Client | Result shape |
| --- | --- |
| .NET | `AppendResult` exposes the appended position and rejection details. `AppendManyResult` carries the batch's sequence numbers and violations. |
| Kotlin and Java | `AppendResult` per event; a batch returns a list. Java can read the position through `getSequenceNumberValue()`. |
| TypeScript | `AppendResult` per event; a batch returns an array. |
| Elixir | `:ok` or an error tuple for the ordinary append APIs, without a returned appended position. |

Inspect the result for kernel constraint and concurrency rejections. This does not mean append APIs never throw: unknown event types, connection failures, and other client or transport failures can still throw or reject. Kotlin also throws `ChronicleCommandRejected` for command-level authorization and exception responses.

Sources: [.NET results](https://github.com/Cratis/Chronicle/tree/main/Source/Clients/DotNET/EventSequences), [JVM result](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/eventSequences/AppendResult.kt), [TypeScript result](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/eventSequences/AppendResult.ts), [Elixir result conversion](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/event_sequences/append_response.ex).

## Compliance and PII

### Resolving the subject on append

| Client | When no explicit append subject is supplied |
| --- | --- |
| .NET | Resolves an event's `[Subject]` property before appending. The kernel falls back to the event source id when no subject is set. |
| Kotlin and Java | Default to the event source id. The append path does not resolve event-side `@Subject` metadata. |
| TypeScript | Uses `options.subject ?? eventSourceId`; the append path does not resolve an event's `@subject` property. |
| Elixir | Sends an empty subject, so the kernel falls back to the event source id. |

Read-model subject metadata and append subject resolution are separate capabilities. When the person whose PII you store differs from the event source, pass the subject explicitly rather than assuming an annotation selects it in every client. Choosing the wrong subject changes whose erasure key protects the data.

Sources: [.NET subject resolver](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/SubjectResolver.cs), [kernel append](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/EventSequences/EventSequence.cs), and each client's append implementation linked above.

### Authorizing a new key after erasure

All five language surfaces expose subject-key deletion. Only .NET and TypeScript expose the kernel operation that permits a new encryption key afterward: `AllowNewEncryptionKeyFor` and `allowNewEncryptionKeyFor`, respectively.

Kotlin, Java, and Elixir do not expose that operation through their client compliance APIs. Workflows that need it must use another supported administrative route or client; deleting a key does not itself authorize a replacement.

Sources: [.NET PII API](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Compliance/GDPR/IPIIManager.cs), [TypeScript PII API](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/compliance/IPIIManager.ts), [JVM compliance API](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/compliance/IComplianceService.kt), [Java bridges](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/java/JavaBridge.kt), [Elixir compliance](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/compliance.ex).

## Reactor replay and delivery identity

### Replay eligibility

| Client | Registration and handler behavior |
| --- | --- |
| .NET | Reactors are replayable unless the class has `[OnceOnly]`. Method-level `[OnceOnly]` skips that handler during replay; `[Replay]` selects a replay handler. |
| Kotlin and Java | Kotlin registration and dispatch use `@OnceOnly` and `@Replay`; Java reactors go through the same JVM registration. |
| TypeScript | Reactor registration sets `IsReplayable: false`; the kernel's replay guards do not start a replay for observers registered as non-replayable. |
| Elixir | Reactor registration sets `IsReplayable: true`. There is no once-only marker. Per-event context has no replay flag, but optional replay-begin/end and partition-replay callbacks report lifecycle transitions. |

These differences follow from registration and dispatch code, together with the kernel's replay guards. Do not treat an available lifecycle callback as proof that the client's registered reactors are replayable.

**Replay exclusion is not exactly-once delivery.** Recovering a failed partition can deliver an event again as an ordinary observation. A side effect still needs idempotency appropriate to the external system.

Sources: [.NET registration](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Reactors/Reactors.cs), [JVM registration](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/observation/ReactorRegistration.kt), [JVM handlers](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/observation/ReactorHandlers.kt), [TypeScript reactors](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/reactors/Reactors.ts), [Elixir handlers](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/reactors/handler.ex), [kernel replay guards](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Observation/Observer.Replay.cs).

### Delivery identity

.NET supplies a `ReactorDelivery` handler parameter with a stable `Id` (of type `DeliveryId`) suitable for an application's receipt key. Kotlin, Java, TypeScript, and Elixir have no equivalent client-provided delivery identity.

Outside .NET, define an idempotency key that distinguishes the event store, namespace, event sequence, reactor, and event position; do not use a sequence number alone across stores or sequences. A key identifies the delivery—it does not make the external effect atomic with recording a receipt.

Sources: [.NET delivery identity](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Reactors/ReactorDelivery.cs), [JVM event context](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/events/EventContext.kt), [TypeScript event context](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/events/EventContext.ts), and the Elixir handler implementation linked above.

## Event seeding

| Client | Default target and duplicate handling | Seeder callback failure |
| --- | --- | --- |
| .NET | `For`/`ForEventSource` entries are global. The kernel applies them to namespaces returned by its namespace inventory at seeding time, tracking entry occurrences by source, event type, content, and tags. | A callback exception propagates. Activation failures are logged and skipped separately. |
| TypeScript | `for`/`forEventSource` entries use the kernel's global seeding path and entry tracking. | Logged as a warning and discovery continues; entries added before the failure are not removed. |
| Kotlin and Java | Entries target the selected namespace, defaulting to the client's namespace. Uses kernel entry tracking, but does not send seed tags. | A callback exception propagates. |
| Elixir | Global entries and entries without an explicit `for_namespace` target the builder's namespace. Appends directly and skips a source that already contains any events, rather than using kernel entry tracking. | Discovery logs and skips the failing seeder, discarding that seeder's partial entries. |

Adding another seed to an existing source can therefore add data through the kernel seeding path but remain unapplied in Elixir. A seed callback completing is also not the same as successful registration; check the owning client's registration outcome separately.

This comparison does not establish when a namespace created later receives previously registered global seeds.

Sources: [.NET seeding](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Seeding/EventSeeding.cs), [kernel seeding](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Seeding/EventSeeding.cs), [JVM seeding](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/seeding/EventSeedingService.kt), [TypeScript seeding](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/seeding/EventSeeding.ts), [Elixir seeding](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/seeding.ex).

For the shared workflows, see [event concurrency](../events/concurrency.mdx), [PII](../compliance/pii.mdx), [reactor delivery identity](../reactors/delivery-identity.mdx), and [event seeding](../event-seeding/index.md).
