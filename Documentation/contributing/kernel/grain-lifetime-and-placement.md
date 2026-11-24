---
title: Grain lifetime and placement in the kernel
description: Why each kernel grain stays resident or is placed on a particular silo, what removing it would break, and how the answer was established.
---

Durable subscriptions (decisions 0034 and 0035) made a deactivated observer recoverable from storage, which raised the question of which `[KeepAlive]` attributes and custom placement strategies the kernel still needs. This page records the audit: one row per `[KeepAlive]` grain and per placement strategy, what breaks without it, and the decision. A new `[KeepAlive]` or placement strategy should be able to fill in the same three columns.

## Grains that stay resident

| Grain | Why it is resident | What breaks without it | Decision |
| --- | --- | --- | --- |
| `Observer` | A subscription exists in the observer activation and in the appended events queue it subscribed to. | A released subscribed observer silently stops its live delivery until something subscribes it again. | Keep. Releasing is not an attribute decision: the observer releases itself when it is dormant (`IsReleasedAsDormant`) or idle (`IsReleasedAsIdle`), which the grain allows by deactivating itself explicitly. Observers whose kind can be woken from a durable record, now including pattern capture, are therefore already collectable. Webhooks and kernel reactors have no record to wake from and stay resident. |
| `AppendedEventsQueue`, `AppendedEventsQueues` | They hold the subscription table the append path fans out to, and the dormant safety net. | A deactivated queue has no subscribers to deliver to until every observer subscribes again. | Keep, until a specification shows the queue restoring its table from durable records on activation without calls from outside. |
| `ProjectionChangesetNotifier` | Its subscriber registrations are in memory only; each live watch renews them by subscribing again. | A deactivation between a client's subscribe and the next notification drops the registration, and the changeset does not reach the watching client until the watch renews. | Keep. Nothing is stored that an activation could rebuild from, so removing the attribute trades a deliberate guarantee for a delivery gap. |
| `ConnectedClients` | One activation per silo tracks the connections the silo terminates and revises them on a schedule. | The registry is lost, which reports every client as disconnected. | Keep. |
| `FederationConsumer`, `AlertIncidentsMetricsOwner` | A grain timer drives the federation link pump, and a periodic aggregation publishes the incident gauge. | The timer stops with the activation; the federation keeper restarts a consumer, but only on its own schedule. | Keep. Neither holds an observer subscription, so durable subscriptions do not change the answer. |

## Placement strategies

| Strategy | What it does | Decision |
| --- | --- | --- |
| `ObserverPlacement` | Places an observer on a random silo that hosts the `Observers` role, preferring silos at least as new as this one. | Keep. It does not tie the observer to its client's stream, so observers already float. What it provides is the role restriction and the release preference, which the default placement ignores. |
| `ConnectedObserverPlacement` | Places a subscriber grain on the silo named in its key, which is the silo that terminates the client's stream. | Keep. See below. |
| `EventSequencePlacement` | Places an event sequence only on silos that host the `EventSequences` role. | Keep. The role restriction is the only reason it exists and the default placement ignores roles. |
| `ServerInstancePlacement` | Places the diagnostics grain on the silo its key names, so the metrics it reports are that silo's. | Keep. Placing it anywhere else makes it report another silo's numbers. |
| `ConnectedClientsPlacement` | Places the connection registry on the silo its key names. | Keep, for the same reason as the attribute above: the registry is the silo's own. |
| `[PreferLocalPlacement]` on `AppendedEventsQueue(s)`, `PatchManager` and `PatternMiner` | Activates the grain on the silo of its first caller. | Keep. It is a hint, not a strategy with a director, and it keeps a per-batch call in process. |

## Why a subscriber grain stays between an observer and a client stream

A connected client reactor or reducer no longer has a grain of its own: the observer delivers to a `ReactorObserverSubscriber` or `ReducerObserverSubscriber`, which hands the batch to the in-process mediator that holds the client's gRPC stream. The mediator is a singleton of the silo that terminates the stream, and the observer is on whichever silo `ObserverPlacement` chose, so the two are on different silos most of the time in a cluster.

Orleans has no way to call into another silo except through a grain, so something addressable on the stream's silo has to receive the batch. The subscriber grain is that receiver, and `ConnectedObserverPlacement` is what puts it there. Replacing it with a per-silo grain would send every observer's deliveries through one activation per silo, which serializes them or has to be made reentrant; the per-observer subscriber grain keeps an observer's batches ordered without coupling it to any other observer. The subscriber grain holds no state and no timer, has no `[KeepAlive]` and is collected when its observer is dormant, so what it costs is one activation per observer that is delivering, for as long as it is.
