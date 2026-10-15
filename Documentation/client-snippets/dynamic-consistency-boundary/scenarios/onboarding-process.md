```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

public record CustomerId(string Value) : EventSourceId(Value);

[EventType]
public record OnboardingStarted(string Email);

[EventType]
public record IdentityChecked(string Method);

// Appending this event closes the customer's Onboarding stream in the same step.
[EventType]
[ClosesStream]
public record OnboardingCompleted;

// One event source per customer, one stream for the onboarding process. Every writer of the
// stream is guarded against every other, whatever the event type.
[EventSource("Customer", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream(
    "Onboarding",
    Description = "The steps of one customer's onboarding",
    Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType)]
public class CustomerEventSource : IEventSource;

public class Onboarding(IEventLog eventLog)
{
    public Task<AppendResult> Start(CustomerId customer, string email) =>
        eventLog.Append<CustomerEventSource>(customer, new OnboardingStarted(email), eventStream: "Onboarding");

    public Task<AppendResult> CheckIdentity(CustomerId customer, string method) =>
        eventLog.Append<CustomerEventSource>(customer, new IdentityChecked(method), eventStream: "Onboarding");

    public async Task<bool> Complete(CustomerId customer)
    {
        var result = await eventLog.Append<CustomerEventSource>(customer, new OnboardingCompleted(), eventStream: "Onboarding");
        return result.IsSuccess;
    }

    // After OnboardingCompleted, any later step is refused with a StreamClosed constraint violation.
    public static bool WasRefusedBecauseClosed(AppendResult result) =>
        result.ConstraintViolations.Any(violation => violation.ConstraintType == ConstraintType.StreamClosed);
}
```
