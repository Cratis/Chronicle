```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType("Customer.Renamed", generation: 2)]
public record CustomerRegisteredV2(string FirstName, string LastName);

[EventTypeGenerationFor<CustomerRegisteredV2>(1)]
public record CustomerRegisteredV1(string Name);

public class CustomerRegisteredMigration
    : EventTypeMigration<CustomerRegisteredV2, CustomerRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<CustomerRegisteredV2, CustomerRegisteredV1> builder) { }
    public override void Downcast(IEventMigrationBuilder<CustomerRegisteredV1, CustomerRegisteredV2> builder) { }
}
```
