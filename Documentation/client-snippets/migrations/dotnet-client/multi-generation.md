```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType]
public record MigrationsDotnetClientMultiGenPersonRegisteredV1(string EmailAddress, string Name);

[EventType("dotnet-client-multi-gen-person-registered", generation: 2)]
public record MigrationsDotnetClientMultiGenPersonRegisteredV2(string Email, string Name);

[EventType("dotnet-client-multi-gen-person-registered", generation: 3)]
public record MigrationsDotnetClientMultiGenPersonRegistered(string Email, string FirstName, string LastName);

// Generation 1 → 2: rename EmailAddress to Email
public class MigrationsDotnetClientMultiGenPersonRegisteredV1ToV2 : EventTypeMigration<MigrationsDotnetClientMultiGenPersonRegisteredV2, MigrationsDotnetClientMultiGenPersonRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientMultiGenPersonRegisteredV2, MigrationsDotnetClientMultiGenPersonRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(t => t.Email, s => s.EmailAddress));

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientMultiGenPersonRegisteredV1, MigrationsDotnetClientMultiGenPersonRegisteredV2> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(t => t.EmailAddress, s => s.Email));
}

// Generation 2 → 3: split Name into FirstName / LastName
public class MigrationsDotnetClientMultiGenPersonRegisteredV2ToV3 : EventTypeMigration<MigrationsDotnetClientMultiGenPersonRegistered, MigrationsDotnetClientMultiGenPersonRegisteredV2>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientMultiGenPersonRegistered, MigrationsDotnetClientMultiGenPersonRegisteredV2> builder) =>
        builder.Properties(pb => pb
            .Split(t => t.FirstName, s => s.Name, PropertySeparator.Space, SplitPartIndex.First)
            .Split(t => t.LastName, s => s.Name, PropertySeparator.Space, SplitPartIndex.Second));

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientMultiGenPersonRegisteredV2, MigrationsDotnetClientMultiGenPersonRegistered> builder) =>
        builder.Properties(pb => pb
            .Combine(t => t.Name, PropertySeparator.Space, s => s.FirstName, s => s.LastName));
}
```
