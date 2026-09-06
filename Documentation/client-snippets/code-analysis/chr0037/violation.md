```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

// Warning CHR0037: Event type generations 'Chr0037ViolationCustomerRegisteredV2' and
// 'Chr0037ViolationCustomerRegisteredV1' referenced by migration
// 'Chr0037ViolationCustomerRegisteredMigration' must resolve to the same event type
// and differ only by generation.
[EventType("Customer.Registered", generation: 1)]
public record Chr0037ViolationCustomerRegisteredV1(string Name);

[EventType("Customer.Renamed", generation: 2)]
public record Chr0037ViolationCustomerRegisteredV2(string FirstName, string LastName);

public class Chr0037ViolationCustomerRegisteredMigration
    : EventTypeMigration<Chr0037ViolationCustomerRegisteredV2, Chr0037ViolationCustomerRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<Chr0037ViolationCustomerRegisteredV2, Chr0037ViolationCustomerRegisteredV1> builder) { }
    public override void Downcast(IEventMigrationBuilder<Chr0037ViolationCustomerRegisteredV1, Chr0037ViolationCustomerRegisteredV2> builder) { }
}
```
