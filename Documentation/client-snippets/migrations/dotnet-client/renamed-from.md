```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType]
public record MigrationsDotnetClientRenamedFromCustomerRegisteredV1(string EmailAddress);

[EventType("dotnet-client-customer-registered", generation: 2)]
public record MigrationsDotnetClientRenamedFromCustomerRegistered(string Email);

public class MigrationsDotnetClientRenamedFromCustomerRegisteredMigration : EventTypeMigration<MigrationsDotnetClientRenamedFromCustomerRegistered, MigrationsDotnetClientRenamedFromCustomerRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientRenamedFromCustomerRegistered, MigrationsDotnetClientRenamedFromCustomerRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(t => t.Email, s => s.EmailAddress));

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientRenamedFromCustomerRegisteredV1, MigrationsDotnetClientRenamedFromCustomerRegistered> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(t => t.EmailAddress, s => s.Email));
}
```
