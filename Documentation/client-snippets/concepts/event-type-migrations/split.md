```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType]
public record MigrationsSplitPersonRegisteredV1(string FullName);

[EventType("person-registered", generation: 2)]
public record MigrationsSplitPersonRegistered(string FirstName, string LastName);

public class MigrationsSplitPersonRegisteredMigration : EventTypeMigration<MigrationsSplitPersonRegistered, MigrationsSplitPersonRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsSplitPersonRegistered, MigrationsSplitPersonRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .Split(m => m.FirstName, e => e.FullName, PropertySeparator.Space, SplitPartIndex.First) // Gets first part
            .Split(m => m.LastName, e => e.FullName, PropertySeparator.Space, SplitPartIndex.Second)); // Gets second part

    public override void Downcast(IEventMigrationBuilder<MigrationsSplitPersonRegisteredV1, MigrationsSplitPersonRegistered> builder) =>
        builder.Properties(pb => pb
            .Combine(m => m.FullName, PropertySeparator.Space, e => e.FirstName, e => e.LastName));
}
```
