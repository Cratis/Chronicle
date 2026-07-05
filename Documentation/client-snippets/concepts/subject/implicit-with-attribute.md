```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

public record SubjectOrderId(Guid Value);
public record SubjectCustomerId(Guid Value);

[EventType]
public record SubjectShippingAddressChangedWithImplicitSubject(
    SubjectOrderId Order,
    [Subject] SubjectCustomerId Customer,
    [PII] string Street,
    [PII] string City);

public class SubjectImplicitSubjectService(IEventStore eventStore)
{
    // Subject is derived from the Customer property — no explicit subject needed.
    public Task ChangeAddress(SubjectOrderId orderId, SubjectCustomerId customerId, string street, string city) =>
        eventStore.EventLog.Append(
            orderId.Value,
            new SubjectShippingAddressChangedWithImplicitSubject(orderId, customerId, street, city));
}
```
