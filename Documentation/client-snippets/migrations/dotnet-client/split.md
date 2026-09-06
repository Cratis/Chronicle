```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType("dotnet-client-person-registered", generation: 2)]
public record MigrationsDotnetClientSplitPersonRegistered(string FirstName, string LastName);

[EventTypeGenerationFor<MigrationsDotnetClientSplitPersonRegistered>(1)]
public record MigrationsDotnetClientSplitPersonRegisteredV1(string FullName);

public class MigrationsDotnetClientSplitPersonRegisteredMigration : EventTypeMigration<MigrationsDotnetClientSplitPersonRegistered, MigrationsDotnetClientSplitPersonRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientSplitPersonRegistered, MigrationsDotnetClientSplitPersonRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .Split(t => t.FirstName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.First)
            .Split(t => t.LastName, s => s.FullName, PropertySeparator.Space, SplitPartIndex.Second));

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientSplitPersonRegisteredV1, MigrationsDotnetClientSplitPersonRegistered> builder) =>
        builder.Properties(pb => pb
            .Combine(t => t.FullName, PropertySeparator.Space, s => s.FirstName, s => s.LastName));
}
```
