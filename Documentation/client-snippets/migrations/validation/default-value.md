```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType]
public record MigrationsValidationAuthorRegisteredV1(string Name);

[EventType("validation-author-registered", generation: 2)]
public record MigrationsValidationAuthorRegistered(string Name, string Status);

public class MigrationsValidationAuthorRegisteredMigration : EventTypeMigration<MigrationsValidationAuthorRegistered, MigrationsValidationAuthorRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsValidationAuthorRegistered, MigrationsValidationAuthorRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .DefaultValue(t => t.Status, "active")); // Name is unchanged between generations — no operation needed for it

    public override void Downcast(IEventMigrationBuilder<MigrationsValidationAuthorRegisteredV1, MigrationsValidationAuthorRegistered> builder)
    {
        // Status does not exist in gen 1 — no mapping needed
    }
}
```
