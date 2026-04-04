---
uid: Chronicle.Events.ObservingAppends
---
# Observing Append Operations

`IEventSequence` exposes an `AppendOperations` observable that emits after every `Append` or
`AppendMany` call on that sequence. Any code can subscribe to this observable to react to appended
events in real time — without polling the event log.

## AppendOperations

```csharp
IObservable<IEnumerable<AppendedEventWithResult>> AppendOperations { get; }
```

The observable emits a collection of `AppendedEventWithResult` after each operation:

- A single-event `Append` emits a collection containing one element.
- A batch `AppendMany` emits the full batch as one collection.

Each `AppendedEventWithResult` pairs the appended event with its result:

| Member | Type | Description |
|---|---|---|
| `Event` | `AppendedEvent` | The appended event, including context and deserialized content |
| `Event.Content` | `object` | The deserialized event object |
| `Event.Context` | `EventContext` | Metadata: event source, sequence number, correlation ID, causation chain |
| `Result` | `AppendResult` | Success flag, sequence number, violations, and errors |

Subscribers receive the notification after the operation has completed, whether it succeeded or failed.
This observable does not fire for transactional appends through `ITransactionalEventSequence`.

## Subscribing

Inject `IEventSequence` (or `IEventLog`) and subscribe to `AppendOperations`:

```csharp
public class AppendMonitor(IEventLog eventLog) : IDisposable
{
    readonly IDisposable _subscription;

    public AppendMonitor()
    {
        _subscription = eventLog.AppendOperations.Subscribe(OnAppended);
    }

    void OnAppended(IEnumerable<AppendedEventWithResult> operations)
    {
        foreach (var item in operations)
        {
            Console.WriteLine($"Event {item.Event.Content.GetType().Name} appended: success={item.Result.IsSuccess}");
        }
    }

    public void Dispose() => _subscription.Dispose();
}
```

Always dispose the subscription when you no longer need it to avoid resource leaks.

## Integration Testing

The most common use of `AppendOperations` in application code is through the
`IEventAppendCollection` helper provided by `Cratis.Chronicle.XUnit.Integration`. It subscribes
internally and provides a ready-to-assert collection of `AppendedEventWithResult` entries.
See [Event Append Collection](event-append-collection.md) for full details.
