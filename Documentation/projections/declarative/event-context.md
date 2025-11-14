# Projection with event context values

Event context contains metadata about each event, such as when it occurred, its sequence number, and causation information. Projections can access these values to enrich read models with event metadata.

## Available event context properties

The `EventContext` contains the following properties you can map to:

- `EventSourceId` - The identifier of the event source that produced the event
- `SequenceNumber` - The position of the event in the event sequence
- `Occurred` - When the event occurred (timestamp)
- `EventType` - Information about the event type
- `EventSourceType` - The type of the event source
- `EventStreamType` - The type of the event stream
- `EventStreamId` - The identifier of the event stream
- `CorrelationId` - The correlation identifier linking related operations
- `Causation` - Information about what caused this event
- `CausedBy` - Identity information about who caused the event

## Mapping to event source ID

Use `ToEventSourceId()` to map the event source ID to a property:

```csharp
public class UserActivityProjection : IProjectionFor<UserActivity>
{
    public void Define(IProjectionBuilderFor<UserActivity> builder) => builder
        .From<UserLoggedIn>(_ => _
            .Set(m => m.UserId).ToEventSourceId()
            .Set(m => m.LastLogin).ToEventContextProperty(c => c.Occurred))
        .From<UserPerformedAction>(_ => _
            .Set(m => m.UserId).ToEventSourceId()
            .Set(m => m.LastActivity).ToEventContextProperty(c => c.Occurred));
}
```

## Mapping to event context properties

Use `ToEventContextProperty()` to map any event context property:

```csharp
public class AuditTrailProjection : IProjectionFor<AuditEntry>
{
    public void Define(IProjectionBuilderFor<AuditEntry> builder) => builder
        .AutoMap()
        .From<UserAction>(_ => _
            .Set(m => m.EventId).ToEventContextProperty(c => c.SequenceNumber)
            .Set(m => m.OccurredAt).ToEventContextProperty(c => c.Occurred)
            .Set(m => m.CorrelationId).ToEventContextProperty(c => c.CorrelationId));
}
```

## Read models with context data

Your read models can include properties for event metadata:

```csharp
public record UserActivity(
    string UserId,
    DateTimeOffset LastLogin,
    DateTimeOffset LastActivity);

public record AuditEntry(
    ulong EventId,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string ActionType,
    string UserId);
```

## Composite keys with event context

Event context properties can be part of composite keys:

```csharp
public class EventLogProjection : IProjectionFor<EventLogEntry>
{
    public void Define(IProjectionBuilderFor<EventLogEntry> builder) => builder
        .From<UserAction>(_ => _
            .UsingCompositeKey<EventLogKey>(_ => _
                .Set(k => k.Date).ToEventContextProperty(c => c.Occurred)
                .Set(k => k.SequenceNumber).ToEventContextProperty(c => c.SequenceNumber))
            .Set(m => m.ActionType).To(e => e.ActionType)
            .Set(m => m.UserId).To(e => e.UserId));
}
```

With the corresponding types:

```csharp
public record EventLogKey(DateTimeOffset Date, ulong SequenceNumber);

public record EventLogEntry(
    EventLogKey Id,
    string ActionType,
    string UserId);
```

## Event context in children and joins

Event context properties work in child collections and joins:

```csharp
.Children(m => m.ActivityLog, children => children
    .IdentifiedBy(e => e.ActivityId)
    .AutoMap()
    .From<ActivityPerformed>(_ => _
        .UsingKey(e => e.ActivityId)
        .Set(m => m.Timestamp).ToEventContextProperty(c => c.Occurred)
        .Set(m => m.SequenceNumber).ToEventContextProperty(c => c.SequenceNumber)))
```

## Event definitions

```csharp
[EventType]
public record UserLoggedIn(string Username);

[EventType]
public record UserPerformedAction(string UserId, string ActionType);

[EventType]
public record UserAction(string UserId, string ActionType);

[EventType]
public record ActivityPerformed(string ActivityId, string ActivityType);
```

## Common use cases

### Audit trails

Map `Occurred`, `SequenceNumber`, and `CausedBy` for complete audit information.

### Debugging and diagnostics

Include `CorrelationId` to trace related operations across services.

### Time-based queries

Use `Occurred` for time-series analysis and historical reporting.

### Event ordering

Use `SequenceNumber` when you need to preserve exact event ordering.

### Source tracking

Use `EventSourceId` to identify which event source produced the event.

Event context properties provide rich metadata that can significantly enhance your read models with timing, traceability, and debugging information.
