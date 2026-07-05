```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType]
public record MigrationsDotnetClientDefaultValueTaskCreatedV1(string Title);

[EventType("dotnet-client-task-created", generation: 2)]
public record MigrationsDotnetClientDefaultValueTaskCreated(string Title, string Status, int RetryCount, bool Enabled);

public class MigrationsDotnetClientDefaultValueTaskCreatedMigration : EventTypeMigration<MigrationsDotnetClientDefaultValueTaskCreated, MigrationsDotnetClientDefaultValueTaskCreatedV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientDefaultValueTaskCreated, MigrationsDotnetClientDefaultValueTaskCreatedV1> builder) =>
        builder.Properties(pb => pb
            .DefaultValue(t => t.Status, "active")
            .DefaultValue(t => t.RetryCount, 0)
            .DefaultValue(t => t.Enabled, true));

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientDefaultValueTaskCreatedV1, MigrationsDotnetClientDefaultValueTaskCreated> builder)
    {
        // Status, RetryCount, and Enabled did not exist in generation 1 — nothing to map back
    }
}
```
