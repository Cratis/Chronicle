---
title: Where the clients differ
description: Differences in appending, concurrency, PII, reactor delivery, and seeding across Chronicle's .NET, Kotlin, Java, TypeScript, and Elixir clients.
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

One kernel contract does not make the client APIs interchangeable. This reference covers appending and concurrency, compliance, reactor delivery, and event seeding. It does not claim parity for other features.

The comparisons below describe the client source inspected on September 23, 2026. The TypeScript rows were checked again on September 30, 2026 against Chronicle.TypeScript v6.31.2, and the Elixir rows on October 15, 2026 against Chronicle.Elixir v3.13.0. Java uses Java-callable interfaces, blocking facades, and bridges that delegate to the Kotlin implementation. Source links identify the implementation behind each comparison. These are source-level comparisons, not cross-client runtime conformance results.

## Appending and concurrency

### Default concurrency checks

| Client | Append without an explicit concurrency scope |
| --- | --- |
| .NET | The default `OptimisticConcurrencyStrategy` reads the event source's tail within the selected stream scope and sends an expected sequence number. `ConcurrencyOptions.DefaultStrategy` can change this behavior. |
| Kotlin and Java | Use `ConcurrencyScope.none`; no optimistic concurrency check is requested. |
| TypeScript | Sends an unset expected sequence number; no optimistic concurrency check is requested. |
| Elixir | Sends no concurrency scope by default. A client configured with `concurrency: [default: :optimistic]` reads the tail, narrowed the way the append is routed, and expects it; `check_first_append: true` also checks the first append into an empty scope. |

The .NET default detects an intervening append between its tail read and write. It does **not** protect an earlier application read automatically. When no event matches the scope, the default leaves the first append unchecked; `CheckFirstAppendIntoAScope` enables that check and defaults to `false`.

Choose an explicit scope when correctness depends on the state you previously read. Do not infer the application's concurrency guarantee from a successful plain append.

Sources: [.NET strategy](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/EventSequences/Concurrency/OptimisticConcurrencyStrategy.cs), [.NET defaults](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/EventSequences/Concurrency/ConcurrencyOptions.cs), [JVM append](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/eventSequences/EventSequence.kt), [TypeScript append](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/eventSequences/EventSequence.ts), [Elixir append](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/event_sequences/event_log.ex), [Elixir default strategy](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/event_sequences/concurrency_strategy.ex).

### Expecting no matching event

.NET exposes `ConcurrencyScopeBuilder.ExpectingNoMatchingEvent`; the JVM builder exposes `withExpectsNoMatchingEvent()`, which its Java bridge documents as directly callable from Java. Both send the kernel's explicit no-matching-event condition. The TypeScript client sends it too, when the scope's sequence number is `EventSequenceNumber.beforeFirst`.

The Elixir client sends it too: `ConcurrencyScope.expecting_no_matching_event/1` sets `expects_no_matching_event`, and the append converter passes it to the kernel as `ExpectsNoMatchingEvent`. An unset expected sequence number is **not** a substitute: it disables the sequence-number check rather than asserting that no event exists.

Sources: [.NET conversion](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/EventSequences/Concurrency/ConcurrencyScopeConverters.cs), [JVM builder](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/eventSequences/concurrency/ConcurrencyScopeBuilder.kt), [kernel validation](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/EventSequences/Concurrency/ConcurrencyValidator.cs), [Elixir scope](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/events/concurrency_scope.ex), and the TypeScript and Elixir append implementations linked above.

### Results and rejections

| Client | Result shape |
| --- | --- |
| .NET | `AppendResult` exposes the appended position and rejection details. `AppendManyResult` carries the batch's sequence numbers and violations. |
| Kotlin and Java | `AppendResult` per event; a batch returns a list. Java can read the position through `getSequenceNumberValue()`. |
| TypeScript | `AppendResult` per event; a batch returns an array. |
| Elixir | `:ok` or an error tuple for `append/3` and `append_many/3`. `append_with_result/3` and `append_many_with_result/3` return an `AppendResult` carrying the assigned sequence numbers, and constraint and concurrency violations; a rejection comes back as `{:error, %AppendResult{}}`. |

Inspect the result for kernel constraint and concurrency rejections. This does not mean append APIs never throw: unknown event types, connection failures, and other client or transport failures can still throw or reject. Kotlin also throws `ChronicleCommandRejected` for command-level authorization and exception responses.

Sources: [.NET results](https://github.com/Cratis/Chronicle/tree/main/Source/Clients/DotNET/EventSequences), [JVM result](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/eventSequences/AppendResult.kt), [TypeScript result](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/eventSequences/AppendResult.ts), [Elixir result](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/event_sequences/append_result.ex), [Elixir result conversion](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/event_sequences/append_response.ex).

### Keyed appends

Only the .NET client has a keyed append API: `AppendOnce`, `AppendManyOnce`, and `IdempotencyKey`. See [Appending](../events/appending#appending-once). The Kotlin, TypeScript, and Elixir clients generate the kernel's keyed RPCs from the shared contracts, but they expose no keyed append method yet. A caller in those clients cannot make an append idempotent through the client API.

## Compliance and PII

### Resolving the subject on append

| Client | When no explicit append subject is supplied |
| --- | --- |
| .NET | Resolves an event's `[Subject]` property before appending. The kernel falls back to the event source id when no subject is set. |
| Kotlin and Java | Default to the event source id. The append path does not resolve event-side `@Subject` metadata. |
| TypeScript | Uses `options.subject ?? eventSourceId`; the append path does not resolve an event's `@subject` property. |
| Elixir | An explicit `:subject` option wins. Otherwise the field named by the event type's `subject:` declaration supplies it; with neither, the client sends an empty subject and the kernel falls back to the event source id. |

Read-model subject metadata and append subject resolution are separate capabilities. When the person whose PII you store differs from the event source, pass the subject explicitly rather than assuming an annotation selects it in every client. Choosing the wrong subject changes whose erasure key protects the data.

Sources: [.NET subject resolver](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/SubjectResolver.cs), [kernel append](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/EventSequences/EventSequence.cs), and each client's append implementation linked above.

### Authorizing a new key after erasure

All five language surfaces expose subject-key deletion. .NET, TypeScript, and Elixir expose the kernel operation that permits a new encryption key afterward: `AllowNewEncryptionKeyFor`, `allowNewEncryptionKeyFor`, and `Chronicle.Compliance.allow_new_encryption_key_for/2`, respectively. Elixir returns `{:error, :not_supported}` from a kernel that lacks the operation.

Kotlin and Java do not expose that operation through their client compliance APIs. Workflows that need it must use another supported administrative route or client; deleting a key does not itself authorize a replacement.

Sources: [.NET PII API](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Compliance/GDPR/IPIIManager.cs), [TypeScript PII API](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/compliance/IPIIManager.ts), [JVM compliance API](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/compliance/IComplianceService.kt), [Java bridges](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/java/JavaBridge.kt), [Elixir compliance](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/compliance.ex).

## Reactor replay and delivery identity

### Replay eligibility

| Client | Registration and handler behavior |
| --- | --- |
| .NET | Reactors are replayable unless the class has `[OnceOnly]`. Method-level `[OnceOnly]` skips that handler during replay; `[Replay]` selects a replay handler. |
| Kotlin and Java | Kotlin registration and dispatch use `@OnceOnly` and `@Replay`; Java reactors go through the same JVM registration. |
| TypeScript | Method-level `@onceOnly()` skips that handler during replay; `@replay()` selects a replay handler. Class-level `@onceOnly()` sets `IsReplayable: false` at registration, but the proto3 encoder leaves a `false` value out, so the kernel registers the reactor as replayable and still replays it. The kernel honors class-level `@onceOnly()` only once the client also sends `IsNotReplayable: true`, tracked in [Chronicle.TypeScript#236](https://github.com/Cratis/Chronicle.TypeScript/issues/236). |
| Elixir | Reactors are replayable unless the module sets `once_only: true`, which registers `IsNotReplayable: true`. `once_only: [EventModule, ...]` skips those events during replay. The event context carries `replaying?`, and an optional `handle_replay/2` is called instead of `handle/2` for replayed events. Optional replay-begin/end and partition-replay callbacks report lifecycle transitions. |

These differences follow from registration and dispatch code, together with the kernel's replay guards. Do not treat an available lifecycle callback as proof that the client's registered reactors are replayable.

**Replay exclusion is not exactly-once delivery.** Recovering a failed partition can deliver an event again as an ordinary observation. A side effect still needs idempotency appropriate to the external system.

Sources: [.NET registration](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Reactors/Reactors.cs), [JVM registration](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/observation/ReactorRegistration.kt), [JVM handlers](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/observation/ReactorHandlers.kt), [TypeScript reactors](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/reactors/Reactors.ts), [Elixir handlers](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/reactors/handler.ex), [Elixir dispatch](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/reactors/dispatch.ex), [kernel replay guards](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Observation/Observer.Replay.cs).

### Delivery identity

.NET supplies a `ReactorDelivery` handler parameter with a stable `Id` (of type `DeliveryId`) suitable for an application's receipt key. Elixir supplies an equivalent on the event context: `delivery_id`, built from the reactor id, event store, namespace, event sequence id, event source id, and sequence number, and identical on every redelivery. Kotlin, Java, and TypeScript have no equivalent client-provided delivery identity.

In those three clients, define an idempotency key that distinguishes the event store, namespace, event sequence, reactor, and event position; do not use a sequence number alone across stores or sequences. A key identifies the delivery—it does not make the external effect atomic with recording a receipt.

Sources: [.NET delivery identity](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Reactors/ReactorDelivery.cs), [JVM event context](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/events/EventContext.kt), [TypeScript event context](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/events/EventContext.ts), and the [Elixir event context](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/events/event_context.ex).

## Event seeding

| Client | Default target and duplicate handling | Seeder callback failure |
| --- | --- | --- |
| .NET | `For`/`ForEventSource` entries are stored globally per event store and applied once to every existing namespace and to each namespace created later. Each client declares discovered globals authoritatively per program identifier, once per event store name per connection lifetime, including empty sets. Explicitly queued globals remain append-only. `ForNamespace(...)` targets only the named namespace. The kernel tracks entry occurrences by source, event type, content, tags, and routing. | Activation and callback failures are logged. If any seeder fails, no discovered global declaration is sent, preserving the previous declaration. Namespace-scoped and manually queued entries still register; the next registration retries discovery. |
| TypeScript | `for`/`forEventSource` entries use the kernel's append-only global seeding path and entry tracking. Authoritative declarations are tracked in [#4871](https://github.com/Cratis/Chronicle/issues/4871). | Logged as a warning and discovery continues; entries added before the failure are not removed. |
| Kotlin and Java | Entries target the selected namespace, defaulting to the client's namespace. Uses kernel entry tracking, but does not send seed tags. | A callback exception propagates. |
| Elixir | Entries without an explicit `for_namespace` target the client's namespace; global entries are applied by the kernel to every namespace it knows about. Entries go to the kernel seeding service, with event type tags, and use its entry tracking, so a new entry added to an already seeded source is applied. There is no authoritative per-program declaration. | A seeder that raises is logged and contributes none of its entries; other seeders still run. A rejected request is retried every five seconds. |

A seed callback completing is not the same as successful registration; check the owning client's registration outcome separately.

In .NET, opening a store for a new namespace creates it. The kernel then applies stored global seeds asynchronously,
usually before the application's first write, but there is no ordering guarantee. Do not rely on that timing;
put development or sample data in `ForNamespace(...)`. With an upgraded .NET client and kernel, removing a discovered
global seed replaces the program's stored declaration, so namespaces created afterwards no longer receive it.
Existing namespaces keep all events already appended. Set distinct, stable `ChronicleOptions.ProgramIdentifier` values
when several programs share a store. The first authoritative declaration clears legacy unowned entries, but retains
other programs' declarations and explicit additions. Identical seeds shared across declarations or explicit additions
use the largest occurrence count in a single program's group, not the sum. Clearing one declaration leaves seeds
still offered by another group available to future namespaces. Older clients and older kernels keep append-only behavior.
See [clear stale development seeds](../event-seeding/seeding-with-csharp.mdx#clear-stale-development-seeds).

Sources: [.NET seeding](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Seeding/EventSeeding.cs), [kernel seeding](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Seeding/EventSeeding.cs), [JVM seeding](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Source/src/main/kotlin/io/cratis/chronicle/seeding/EventSeedingService.kt), [TypeScript seeding](https://github.com/Cratis/Chronicle.TypeScript/blob/main/Source/seeding/EventSeeding.ts), [Elixir seeding](https://github.com/Cratis/Chronicle.Elixir/blob/main/Source/chronicle/lib/chronicle/seeding.ex).

For the shared workflows, see [event concurrency](../events/concurrency.mdx), [PII](../compliance/pii.mdx), [reactor delivery identity](../reactors/delivery-identity.mdx), and [event seeding](../event-seeding/index.md).
