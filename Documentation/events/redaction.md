# Event Redaction

Redaction removes the meaningful content of a previously appended event when you need to erase data — for example, to comply with a right-to-erasure request. Rather than deleting the event entirely, Chronicle replaces the event's payload with a redaction marker that preserves the sequence number, original event type, and the full audit context of both the original event and the redaction action itself.

## Why redaction keeps the event slot

Events are immutable, and their sequence numbers are permanent identifiers. Observers, projections, and reducers may have already processed an event and advanced their positions based on those sequence numbers. Removing the slot entirely would corrupt every downstream position. Redaction instead replaces the content in-place, changes the event type to the internal `EventRedacted` type, and stores the reason and the original context alongside it.

This means:

- Observers that replay the event see only the redaction marker, not the original data.
- Positions and sequence numbers remain stable and consistent.
- An auditor can still determine that an event existed at that position and who requested its removal.

## When to use redaction

Redact an event when:

- A regulation or legal request (e.g. GDPR right to erasure) requires that personal data be removed.
- An event was appended in error and contains sensitive information that must not be retained.
- You need to wipe all events associated with a specific event source (e.g. a user account deletion).

Do not use redaction as a way to undo domain decisions. If a correction is being made in the domain — for example, a cancellation, reversal, or override — model that as a new event instead.

## Redacting a single event

To redact one specific event by its sequence number, call `Redact` on the event sequence and provide a reason:

```csharp
await eventLog.Redact(sequenceNumber, RedactionReason.Unknown);
```

Always provide a meaningful reason when possible:

```csharp
await eventLog.Redact(sequenceNumber, new RedactionReason("GDPR erasure request"));
```

## Redacting all events for an event source

To redact every event associated with a particular event source — for example, to erase all data for a specific user — pass the event source identifier and a reason:

```csharp
await eventLog.Redact(eventSourceId, new RedactionReason("Account deletion requested"));
```

If you only want to redact a specific type of event rather than all events for the event source, provide the event types:

```csharp
await eventLog.Redact(eventSourceId, new RedactionReason("PII erasure"), typeof(PersonalDetailsRecorded), typeof(AddressChanged));
```

## How it works

Redaction is modelled as a system event in the system event sequence. When you call `Redact`, Chronicle appends an `EventRedactionRequested` (for a single event) or an `EventsRedactedForEventSource` (for an event source) system event to the internal system sequence. A built-in reactor observes these system events and performs the actual in-place replacement in the target event sequence.

The replacement stores an `EventRedacted` content record inside the event document that captures:

- The reason for the redaction.
- The original event type, so auditors know what kind of event was removed.
- The `Occurred`, `CorrelationId`, `Causation`, and `CausedBy` of the **original** event, preserving its full provenance.

The event's own context fields are then updated to reflect the time and identity of the redaction action, giving a complete two-sided audit record.

## Observing redacted events in Reactors and Reducers

When an event is redacted, the `EventRedacted` event is dispatched to any observer that was subscribed to the original event type and also includes `EventRedacted` in its subscription. This lets Reactors and Reducers react to the removal of data — for example, to clean up read models or trigger downstream notifications.

### Why subscribe to `EventRedacted`?

- **Read model cleanup**: If a Reducer builds state from an event that is later redacted, subscribing to `EventRedacted` lets you remove or anonymise that state in reaction.
- **Audit and notifications**: A Reactor can notify external systems or write a compliance log entry whenever sensitive data is erased.
- **Cross-aggregate side effects**: If redacting one event should trigger cancellations or clean-up in other parts of the system, a Reactor can coordinate that work.

### Subscribing in a Reactor

Add a handler method that accepts `EventRedacted` alongside the event type(s) you already handle. The Kernel will only dispatch `EventRedacted` to your Reactor when the redacted event belongs to a type that your Reactor is also subscribed to.

```csharp
public class PersonReactor : IReactor
{
    public Task OnPersonRegistered(PersonRegistered evt, EventContext ctx)
    {
        // Handle the original event
        return Task.CompletedTask;
    }

    public Task OnEventRedacted(EventRedacted evt, EventContext ctx)
    {
        // Called only when a PersonRegistered event (a type this reactor handles) is redacted.
        // evt.OriginalEventType is typeof(PersonRegistered).
        // Use this to undo any side effects produced by the original event.
        return Task.CompletedTask;
    }
}
```

The `EventRedacted` record is defined in `Cratis.Chronicle.Events` and carries:

| Property | Description |
|---|---|
| `Reason` | The `RedactionReason` provided at the time of redaction. |
| `OriginalEventType` | The CLR `Type` of the event that was redacted. |
| `Occurred` | When the original event occurred. |
| `CorrelationId` | The correlation identifier of the original event. |
| `Causation` | The full causation chain of the original event. |
| `CausedBy` | The identity chain that caused the original event. |

### Subscribing in a Reducer

The same approach applies to Reducers. Include `EventRedacted` handling inside the `IReducerFor<TReadModel>` implementation to update the read model when a source event is erased.

```csharp
public class PersonReadModelReducer : IReducerFor<PersonReadModel>
{
    public PersonReadModel OnPersonRegistered(PersonRegistered evt, PersonReadModel? current, EventContext ctx) =>
        (current ?? new PersonReadModel()) with
        {
            Name = evt.Name
        };

    public PersonReadModel? OnEventRedacted(EventRedacted evt, PersonReadModel? current, EventContext ctx) =>
        // Return null to remove the read model, or return a sanitised version
        null;
}
```

### Filtering guarantee

The `EventRedacted` event is delivered to an observer **only if** the observer was already subscribed to the original event type. An observer that subscribes to `[PersonRegistered, EventRedacted]` will **not** receive an `EventRedacted` notification when an unrelated event type (e.g. `OrderPlaced`) is redacted.



The Workbench lets you redact events without writing any code:

1. Open the **Event Log** view for the event store and namespace you want to manage.
2. Select the event you want to redact.
3. Click the **Redact** button in the event detail panel.
4. In the dialog that appears, enter the reason for the redaction.
5. Click **OK** to confirm.

Chronicle immediately replaces the event's content with the redaction marker. The event slot remains visible in the sequence but no longer contains the original data.
