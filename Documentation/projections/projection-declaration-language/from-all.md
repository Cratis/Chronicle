# All Block

The `all` keyword makes the projection subscribe to **every event type in the system**, regardless of the explicit `from` blocks listed in the projection. Mappings defined inside `all` run for every event that arrives, across all event types.

This is distinct from `every`, which only runs for event types the projection **explicitly subscribes to** through `from` blocks.

## Basic Syntax

```pdl
all
  {mappings}
```

## Simple Example

```pdl
projection EventCounter => EventCounterReadModel
  all
    count totalEvents
    lastOccurred = $eventContext.occurred
```

The projection above receives **all events in the system** — even event types not listed in any `from` block — and increments `totalEvents` for each one.

## Combining `all` with `from` Blocks

You can combine `all` with explicit `from` blocks:

```pdl
projection ActivityFeed => ActivityFeedModel
  all
    count totalSystemEvents
    lastActivity = $eventContext.occurred

  from UserRegistered
    recentUsers = name
```

Here, `totalSystemEvents` increments for every event in the system, while `recentUsers` is only set when a `UserRegistered` event arrives.

## Difference Between `all` and `every`

| Feature | `all` | `every` |
|---------|--------|---------|
| Event scope | **All event types in the system** | Only types listed in `from` blocks |
| Subscription mechanism | `SubscribeToAllEvents` (implicit, system-wide) | Explicit per-type subscription |
| Typical use | System-wide audit logs, global counters | Common fields across explicitly subscribed events |

### Example contrast

```pdl
# Uses 'every' — only runs for UserRegistered and UserEmailChanged
projection UserProfile => UserProfileModel
  every
    LastUpdated = $eventContext.occurred

  from UserRegistered
    Name = name

  from UserEmailChanged
    Email = email
```

```pdl
# Uses 'all' — runs for every event type in the entire system
projection SystemAuditLog => AuditLogModel
  all
    count totalEvents
    lastEventAt = $eventContext.occurred
```

## When to Use

**Use `all` when:**

- Building system-wide audit logs or metrics
- Tracking total event counts across all types
- Updating timestamps based on any activity in the system

**Use `every` instead when:**

- Only events the projection explicitly subscribes to should trigger the mapping
- You want common fields across your own `from` blocks, not unrelated system events
