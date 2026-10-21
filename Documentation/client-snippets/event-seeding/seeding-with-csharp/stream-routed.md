```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Seeding;

public sealed class EvtStreamRoutedSeeder : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For("company-123", "Offices", "office-1", [
            new EvtStreamRoutedOfficeOpened("Oslo")
        ], "Company");

        builder.ForNamespace("tenant-a").ForEventSource(
            "company-123", "Offices", "office-2", [
                new EvtStreamRoutedOfficeOpened("Bergen")
            ], "Company");
    }
}

[EventType]
public record EvtStreamRoutedOfficeOpened(string City);
```
