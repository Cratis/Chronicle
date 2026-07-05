```csharp
using Cratis.Chronicle.Events;

public record ModelingEventsCustomerName(string Value);
public record ModelingEventsEmail(string Value);
public record ModelingEventsDeactivationReason(string Value);
public record ModelingEventsCustomerAddress(string Street, string City);

// One event trying to be everything — consumers must guess what changed
[EventType]
public record ModelingEventsCustomerUpdated(
    string? Name,
    ModelingEventsCustomerAddress? Address,
    ModelingEventsEmail? Email,
    bool? Deactivated);

// Distinct facts — each consumer subscribes to exactly what it cares about
[EventType]
public record ModelingEventsCustomerRenamed(ModelingEventsCustomerName Name);

[EventType]
public record ModelingEventsCustomerAddressChanged(ModelingEventsCustomerAddress Address);

[EventType]
public record ModelingEventsCustomerDeactivated(ModelingEventsDeactivationReason Reason);
```
