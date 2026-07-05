```csharp
using Cratis.Chronicle.Events.Migrations;

public class MigrationsDotnetClientAuthorRegisteredMigration : EventTypeMigration<MigrationsDotnetClientAuthorRegistered, MigrationsDotnetClientAuthorRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientAuthorRegistered, MigrationsDotnetClientAuthorRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .Split(m => m.FirstName, e => e.Name, PropertySeparator.Space, SplitPartIndex.First)
            .Split(m => m.LastName, e => e.Name, PropertySeparator.Space, SplitPartIndex.Second));

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientAuthorRegisteredV1, MigrationsDotnetClientAuthorRegistered> builder) =>
        builder.Properties(pb => pb
            .Combine(m => m.Name, PropertySeparator.Space, e => e.FirstName, e => e.LastName));
}
```
