---
uid: Chronicle.Testing.EventAppendCollection
---
# Event Append Collection

`IEventAppendCollection` is a scoped collector that captures every event appended to the event log
while it is active. It is the primary tool for asserting on appended events in integration tests.

## How It Works

Calling `StartCollectingAppends()` on the fixture subscribes a new `IEventAppendCollection` to the
`AppendOperations` observable on the event log. Every subsequent call to `EventStore.EventLog.Append`
or `EventStore.EventLog.AppendMany` is captured automatically and stored in the collection as an
`AppendedEventWithResult`. This includes events appended by reactors that use
`ICommandPipeline.Execute()` — because the command pipeline commits its unit of work through the
same `AppendMany` path, those appends are captured just like direct ones.

Because `Append` is awaited, the append operation is complete and the event is recorded in the
collection by the time the `await` returns. When testing reactors that append follow-up events
asynchronously, use `WaitForCount()` to wait for the expected number of events to arrive.

## Scope Lifetime

Create a scope immediately before the operation under test so no earlier events are captured. Dispose
it when the operation is done. The recommended pattern is:

```csharp
IEventAppendCollection _appendedEventsCollector;

async Task Because()
{
    _appendedEventsCollector = StartCollectingAppends();
    await EventStore.EventLog.Append(EventSourceId, new ItemRegistered("Widget"));
}

void Destroy() => _appendedEventsCollector?.Dispose();
```

Disposal unsubscribes the collection immediately. Any appends that occur after disposal are not
captured, which prevents tests sharing a fixture from interfering with each other.

## AppendedEventWithResult Members

Each entry in `All` is an `AppendedEventWithResult` record that pairs the appended event with the
full outcome of the operation:

| Member | Type | Description |
|---|---|---|
| `Event` | `AppendedEvent` | The appended event, including its context and deserialized content |
| `Event.Content` | `object` | The deserialized event object (cast to your event type for assertions) |
| `Event.Context` | `EventContext` | Metadata: event source, sequence number, correlation ID, causation chain, etc. |
| `Event.Context.EventSourceId` | `EventSourceId` | The event source the event was appended for |
| `Event.Context.SequenceNumber` | `EventSequenceNumber` | Assigned sequence number |
| `Event.Context.CorrelationId` | `CorrelationId` | Correlation ID active at the time of the append |
| `Event.Context.Causation` | `IEnumerable<Causation>` | The causation chain active at the time of the append |
| `Result` | `AppendResult` | The outcome of the append operation |
| `Result.IsSuccess` | `bool` | `true` when the sequence number is valid and there are no violations or errors |
| `Result.SequenceNumber` | `EventSequenceNumber` | Assigned sequence number; `EventSequenceNumber.Unavailable` when the append failed |
| `Result.HasConstraintViolations` | `bool` | `true` when at least one constraint violation was returned |
| `Result.HasConcurrencyViolations` | `bool` | `true` when at least one concurrency violation was returned |
| `Result.HasErrors` | `bool` | `true` when at least one error was returned |
| `Result.ConstraintViolations` | `IEnumerable<ConstraintViolation>` | Constraint violations, if any |
| `Result.ConcurrencyViolation` | `ConcurrencyViolation?` | Concurrency violation, if any |
| `Result.Errors` | `IEnumerable<AppendError>` | Errors, if any |

## Asserting on Collected Events

`IEventAppendCollection` exposes three members:

| Member | Description |
|---|---|
| `All` | A snapshot of every `AppendedEventWithResult` captured so far |
| `Last` | The most recently captured `AppendedEventWithResult`; throws when nothing has been collected |
| `WaitForCount(count, timeout?)` | Waits asynchronously until at least `count` events have been collected |

### Single event

```csharp
[Fact] void should_collect_one_event() => Context._appendedEventsCollector.All.Count.ShouldEqual(1);
[Fact] void should_have_appended_the_event() => Context._appendedEventsCollector.All[0].Event.Content.ShouldBeOfExactType<ItemRegistered>();
[Fact] void should_be_successful() => Context._appendedEventsCollector.All[0].Result.IsSuccess.ShouldBeTrue();
[Fact] void should_have_a_valid_sequence_number() => Context._appendedEventsCollector.All[0].Result.SequenceNumber.IsActualValue.ShouldBeTrue();
```

### Multiple events

When a reactor handles an event and appends a follow-up, all appends land in the same collection.
Use LINQ to locate the event you want:

```csharp
AppendedEventWithResult FollowUp => Context._appendedEventsCollector.All.First(e => e.Event.Content is FollowUpAppended);
```

## Waiting for Asynchronous Appends

When a reactor appends events after handling an incoming event, those appends happen asynchronously
on the server. Use `WaitForCount()` to wait for the expected number of events to arrive before
asserting:

```csharp
async Task Because()
{
    var reactor = EventStore.Reactors.GetHandlerFor<ShipmentReactor>();
    await reactor.WaitTillActive();

    _appendedEventsCollector = StartCollectingAppends();
    await EventStore.EventLog.Append(EventSourceId, new OrderPlaced("order-123"));

    // Wait for the reactor's follow-up append to arrive
    await _appendedEventsCollector.WaitForCount(2);
}
```

`WaitForCount` accepts an optional `TimeSpan` timeout (default: 5 seconds) and throws
`TimeoutException` if the expected count is not reached in time.

## Checking Violations

When a reactor appends directly and the append is rejected by a constraint, the violation is captured
on the `AppendedEventWithResult.Result`:

```csharp
[Fact] void should_have_a_constraint_violation() =>
    Context._appendedEventsCollector.All[0].Result.HasConstraintViolations.ShouldBeTrue();
```

To find the first violation among multiple appends:

```csharp
AppendedEventWithResult ViolatingAppend => Context._appendedEventsCollector.All.First(e => e.Result.HasConstraintViolations);
```

## Full Example

The following is a complete integration test that verifies a reactor that listens for `OrderPlaced`
and appends a `ShipmentScheduled` follow-up event.

**Events and reactor:**

```csharp
[EventType]
public record OrderPlaced(string OrderId);

[EventType]
public record ShipmentScheduled(string OrderId);

public class ShipmentReactor(IEventLog eventLog) : IReactor
{
    public Task OnOrderPlaced(OrderPlaced evt, EventContext ctx) =>
        eventLog.Append(ctx.EventSourceId, new ShipmentScheduled(evt.OrderId));
}
```

**Given context** — shared setup for the test group:

```csharp
namespace MyApp.Integration.for_ShipmentReactor.given;

public class a_shipment_reactor_context(ChronicleInProcessFixture fixture) : Specification(fixture)
{
    public EventSourceId EventSourceId;
    public IEventAppendCollection _appendedEventsCollector;

    public override IEnumerable<Type> EventTypes =>
        [typeof(OrderPlaced), typeof(ShipmentScheduled)];
    public override IEnumerable<Type> Reactors =>
        [typeof(ShipmentReactor)];

    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddSingleton<ShipmentReactor>();

    void Establish() => EventSourceId = EventSourceId.New();

    void Destroy() => _appendedEventsCollector?.Dispose();
}
```

**Spec:**

```csharp
namespace MyApp.Integration.for_ShipmentReactor.when_an_order_is_placed;

[Collection(ChronicleCollection.Name)]
public class and_collecting_the_scheduled_shipment(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_shipment_reactor_context(fixture)
    {
        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ShipmentReactor>();
            await reactor.WaitTillActive();

            _appendedEventsCollector = StartCollectingAppends();
            await EventStore.EventLog.Append(EventSourceId, new OrderPlaced("order-123"));

            // Wait for the reactor's follow-up append
            await _appendedEventsCollector.WaitForCount(2);
        }
    }

    AppendedEventWithResult Shipment => Context._appendedEventsCollector.All
        .First(e => e.Event.Content is ShipmentScheduled);

    [Fact] void should_schedule_a_shipment() =>
        Shipment.Event.Content.ShouldBeOfExactType<ShipmentScheduled>();
    [Fact] void should_carry_the_order_id() =>
        ((ShipmentScheduled)Shipment.Event.Content).OrderId.ShouldEqual("order-123");
    [Fact] void should_be_successful() =>
        Shipment.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_a_valid_sequence_number() =>
        Shipment.Result.SequenceNumber.IsActualValue.ShouldBeTrue();
}
```

`WaitTillActive()` ensures the reactor is registered and listening on the server before the test
appends the triggering event. `WaitForCount(2)` then waits for both the original event and the
reactor's follow-up to be captured before assertions run.
