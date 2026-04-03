# Event Append Collection

`IEventAppendCollection` is a scoped collector that captures every event appended to the event log
while it is active. It is the primary tool for asserting on appended events in integration tests.

## How It Works

Calling `StartCollectingAppends()` on the fixture subscribes a new `IEventAppendCollection` to the event log.
Every subsequent call to `EventStore.EventLog.Append` or `EventStore.EventLog.AppendMany` is
captured automatically and stored in the collection. This includes events appended by reactors that
use `ICommandPipeline.Execute()` — because the command pipeline commits its unit of work through the
same `AppendMany` path, those appends are captured just like direct ones.

Because `Append` is awaited, the append operation is complete and the event is recorded in the
collection by the time the `await` returns. No polling or explicit waiting is needed.

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

## Asserting on Collected Events

`IEventAppendCollection` exposes two members:

| Member | Description |
|---|---|
| `All` | A snapshot of every `CollectedEvent` captured so far |
| `Last` | The most recently captured `CollectedEvent`; throws when nothing has been collected |

### Single event

```csharp
    [Fact] void should_collect_one_event() => Context._appendedEventsCollector.All.Count.ShouldEqual(1);
    [Fact] void should_have_appended_the_event() => Context._appendedEventsCollector.All[0].Event.ShouldBeOfExactType<ItemRegistered>();
    [Fact] void should_be_successful() => Context._appendedEventsCollector.All[0].IsSuccess.ShouldBeTrue();
    [Fact] void should_have_a_valid_sequence_number() => Context._appendedEventsCollector.All[0].SequenceNumber.IsActualValue.ShouldBeTrue();
```

### Multiple events

When a reactor handles an event and appends a follow-up, all appends land in the same collection.
Use LINQ to locate the event you want:

```csharp
CollectedEvent FollowUp => Context._appendedEventsCollector.All.First(e => e.Event is FollowUpAppended);
```

## CollectedEvent Members

Each entry in `All` is a `CollectedEvent` record that captures the full outcome of one append call:

| Member | Type | Description |
|---|---|---|
| `Event` | `object` | The event object that was appended (or attempted) |
| `EventSourceId` | `EventSourceId` | The event source the event was appended for |
| `SequenceNumber` | `EventSequenceNumber` | Assigned sequence number; `EventSequenceNumber.Unavailable` when the append failed |
| `CorrelationId` | `CorrelationId` | Correlation ID active at the time of the append |
| `CausationChain` | `IImmutableList<Causation>` | The causation chain active at the time of the append |
| `IsSuccess` | `bool` | `true` when the sequence number is valid and there are no violations or errors |
| `HasConstraintViolations` | `bool` | `true` when at least one constraint violation was returned |
| `HasConcurrencyViolations` | `bool` | `true` when at least one concurrency violation was returned |
| `HasErrors` | `bool` | `true` when at least one error was returned |
| `ConstraintViolations` | `IEnumerable<ConstraintViolation>` | Constraint violations, if any |
| `ConcurrencyViolations` | `IEnumerable<ConcurrencyViolation>` | Concurrency violations, if any |
| `Errors` | `IEnumerable<AppendError>` | Errors, if any |

## Checking Violations

When a reactor appends directly and the append is rejected by a constraint, the violation is captured
on the `CollectedEvent`:

```csharp
[Fact] void should_have_a_constraint_violation() =>
    Context._appendedEventsCollector.All[0].HasConstraintViolations.ShouldBeTrue();
```

To find the first violation among multiple appends:

```csharp
CollectedEvent ViolatingAppend => Context._appendedEventsCollector.All.First(e => e.HasConstraintViolations);
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
        }
    }

    CollectedEvent Shipment => Context._appendedEventsCollector.All
        .First(e => e.Event is ShipmentScheduled);

    [Fact] void should_schedule_a_shipment() =>
        Shipment.Event.ShouldBeOfExactType<ShipmentScheduled>();
    [Fact] void should_carry_the_order_id() =>
        ((ShipmentScheduled)Shipment.Event).OrderId.ShouldEqual("order-123");
    [Fact] void should_be_successful() =>
        Shipment.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_a_valid_sequence_number() =>
        Shipment.SequenceNumber.IsActualValue.ShouldBeTrue();
}
```

`WaitTillActive()` ensures the reactor is registered and listening on the server before the test
appends the triggering event. Once `Append` returns, the follow-up event is already in the
collection and assertions run immediately with no polling or delays.
