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
| `queues` | Number of appended-event queues, each with its own asynchronous processing loop. More queues allow more observer parallelism but consume more resources. |
| `queueBoundedCapacity` | Capacity of each queue in **batches**, not individual events. `0` makes the queue unbounded. |
| `statePersistenceInterval` | Appends between warm-start state snapshots. State is also persisted on the first append after activation and on deactivation. |
| `constraintsVersionCheckInterval` | Maximum frequency of constraint-version polling during appends. A zero interval checks on every append. |
| `queueDepletionWaitTimeoutMilliseconds` | Bounds the final queue-empty wait when awaiting depletion outside a debugger. |

When a bounded queue fills, appends do not wait for observers to drain it. Its observers spill to catch-up, which rereads from their persisted cursors. Increase capacity to absorb longer bursts; smaller queues trade live delivery for catch-up sooner. The state snapshot interval does not determine sequence-number correctness: activation can rebuild state from the event tail.

## Causation property retention

`causationPropertyRetention` accepts `Retain` or `Omit`. `Omit` preserves each causation entry's type and time but writes an empty properties dictionary for newly persisted events. It does not rewrite historical events or remove personal data from event payloads.

To override only this setting through the environment, use `Cratis__Chronicle__Events__CausationPropertyRetention=Omit`.

## See also

- [Configuration precedence](configuration-precedence.md)
- [Environment variables](environment-variables.md)
- [Observers](observers.md)
- [Compliance](../../compliance/index.md)
