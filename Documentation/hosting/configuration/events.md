---
description: Configure Chronicle event queues, snapshot persistence, constraint checks, and causation property retention in chronicle.json or environment variables.
---
# Events

The `events` section controls how Chronicle delivers appended events to observers and retains event-sequence state and causation properties. Use it to tune burst handling or reduce persisted audit metadata without changing your application's event types.

## Example configuration

Put these settings at the root of `chronicle.json`, not inside a `Cratis` or `Chronicle` wrapper. The following example shows the defaults:

```json
{
    "events": {
        "queues": 2,
        "queueBoundedCapacity": 2000,
        "statePersistenceInterval": 1000,
        "constraintsVersionCheckInterval": "00:00:01",
        "causationPropertyRetention": "Retain",
        "queueDepletionWaitTimeoutMilliseconds": 500
    }
}
```

| Setting | Meaning |
| --- | --- |
| `queues` | Number of appended-event queues per event sequence. Each queue routes appended events to the observers assigned to it. See [How observers use the queues](#how-observers-use-the-queues). |
| `queueBoundedCapacity` | Capacity in **batches**, not individual events, of each queue and of each observer's backlog within it. `0` makes both unbounded. |
| `statePersistenceInterval` | Appends between warm-start state snapshots. State is also persisted on the first append after activation and on deactivation. |
| `constraintsVersionCheckInterval` | Maximum frequency of constraint-version polling during appends. A zero interval checks on every append. |
| `queueDepletionWaitTimeoutMilliseconds` | Bounds the final queue-empty wait when awaiting depletion outside a debugger. |

The state snapshot interval does not determine sequence-number correctness: activation can rebuild state from the event tail.

## How observers use the queues

Every reactor, reducer, projection and other observer of an event sequence is assigned to one of its queues by a stable hash of the observer's id. The same id always lands on the same queue, across restarts, for a given `queues` value. When events are appended, each queue passes them to the observers assigned to it that subscribe to them. This is live delivery only; catch-up and replay read from the event log and do not go through the queues.

Within a queue, every observer has its own backlog and receives its events at its own pace. A reactor waiting on a slow downstream service holds back only itself. A reducer or projection assigned to the same queue keeps handling new events as they arrive. Each observer gets its events in sequence order, one event source at a time. When it has fallen behind, its backlog is delivered in fewer, larger calls.

Appends never wait for observers:

- When one observer's backlog exceeds `queueBoundedCapacity` batches, that observer alone stops receiving live events and spills to catch-up. Catch-up rereads what it missed from its persisted cursor, and the observer subscribes again once it has caught up. The other observers on the queue are not affected.
- When a queue itself fills, all its observers spill to catch-up the same way.

Increase `queueBoundedCapacity` to absorb longer bursts. A smaller value moves a lagging observer to catch-up sooner.

More queues spread the routing of appended events over more processing loops, which helps when many observers share an event sequence. More queues are not needed to keep a slow observer from delaying others.

### Throughput of a single observer

An observer handles one event source's run of consecutive events per call, and its calls happen one after the other. Its throughput therefore depends on how the events arrive more than on how many there are:

- Events for the same event source appended together reach the observer in one call.
- Events from many event sources interleaved in the log reach it in one call each.

The cost of a call is mostly the round trip to the client and the observer's own progress write. When a burst interleaves many event sources, an observer handles far fewer events per second than when the same number of events arrives grouped by event source.

## Causation property retention

`causationPropertyRetention` accepts `Retain` or `Omit`. `Omit` preserves each causation entry's type and time but writes an empty properties dictionary for newly persisted events. It does not rewrite historical events or remove personal data from event payloads.

To override only this setting through the environment, use `Cratis__Chronicle__Events__CausationPropertyRetention=Omit`.

## See also

- [Configuration precedence](configuration-precedence.md)
- [Environment variables](environment-variables.md)
- [Observers](observers.md)
- [Compliance](../../compliance/index.md)
