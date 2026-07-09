```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

// Warning CHR0037: Event type generations 'CustomerRegisteredV2' and 'CustomerRegisteredV1'
// referenced by migration 'CustomerRegisteredMigration' must share one explicit [EventType]
// id and differ only by generation.
[EventType("Customer.Registered", generation: 1)]
public record CustomerRegisteredV1(string Name);

[EventType("Customer.Renamed", generation: 2)]
public record CustomerRegisteredV2(string FirstName, string LastName);

public class CustomerRegisteredMigration
    : EventTypeMigration<CustomerRegisteredV2, CustomerRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<CustomerRegisteredV2, CustomerRegisteredV1> builder) { }
    public override void Downcast(IEventMigrationBuilder<CustomerRegisteredV1, CustomerRegisteredV2> builder) { }
}
```
