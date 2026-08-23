```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType("Customer.Renamed", generation: 2)]
public record Chr0037FixCustomerRegisteredV2(string FirstName, string LastName);

[EventTypeGenerationFor<Chr0037FixCustomerRegisteredV2>(1)]
public record Chr0037FixCustomerRegisteredV1(string Name);

public class Chr0037FixCustomerRegisteredMigration
    : EventTypeMigration<Chr0037FixCustomerRegisteredV2, Chr0037FixCustomerRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<Chr0037FixCustomerRegisteredV2, Chr0037FixCustomerRegisteredV1> builder) { }
    public override void Downcast(IEventMigrationBuilder<Chr0037FixCustomerRegisteredV1, Chr0037FixCustomerRegisteredV2> builder) { }
}
```
