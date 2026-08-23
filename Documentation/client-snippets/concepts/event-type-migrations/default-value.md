```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType("order-shipped", generation: 2)]
public record MigrationsDefaultValueOrderShipped(string TrackingNumber, int RetryCount, string Description);

[EventTypeGenerationFor<MigrationsDefaultValueOrderShipped>(1)]
public record MigrationsDefaultValueOrderShippedV1(string TrackingNumber);

public class MigrationsDefaultValueOrderShippedMigration : EventTypeMigration<MigrationsDefaultValueOrderShipped, MigrationsDefaultValueOrderShippedV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDefaultValueOrderShipped, MigrationsDefaultValueOrderShippedV1> builder) =>
        builder.Properties(pb => pb
            .DefaultValue(m => m.RetryCount, 42)
            .DefaultValue(m => m.Description, "default string"));

    public override void Downcast(IEventMigrationBuilder<MigrationsDefaultValueOrderShippedV1, MigrationsDefaultValueOrderShipped> builder)
    {
        // RetryCount and Description did not exist in generation 1 — nothing to map back
    }
}
```
