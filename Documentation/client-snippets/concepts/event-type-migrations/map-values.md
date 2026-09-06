```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

public enum MigrationsSubscriptionStateV1
{
    Unknown = 0,
    Active = 1,
    Cancelled = 2
}

public enum MigrationsSubscriptionState
{
    Unspecified = 100,
    Running = 101,
    Stopped = 102
}

[EventType("subscription-state-changed", generation: 2)]
public record MigrationsSubscriptionStateChanged(MigrationsSubscriptionState State);

[EventTypeGenerationFor<MigrationsSubscriptionStateChanged>(1)]
public record MigrationsSubscriptionStateChangedV1(MigrationsSubscriptionStateV1 State);

public class MigrationsSubscriptionStateChangedMigration : EventTypeMigration<MigrationsSubscriptionStateChanged, MigrationsSubscriptionStateChangedV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsSubscriptionStateChanged, MigrationsSubscriptionStateChangedV1> builder)
    {
        // The value map covers State in both directions
    }

    public override void Downcast(IEventMigrationBuilder<MigrationsSubscriptionStateChangedV1, MigrationsSubscriptionStateChanged> builder)
    {
        // The value map covers State in both directions
    }

    public override void MapValues(IEventValueMapBuilder<MigrationsSubscriptionStateChanged, MigrationsSubscriptionStateChangedV1> builder) =>
        builder.For(current => current.State, previous => previous.State, map => map
            .Map(MigrationsSubscriptionStateV1.Unknown, MigrationsSubscriptionState.Unspecified)
            .Map(MigrationsSubscriptionStateV1.Active, MigrationsSubscriptionState.Running)
            .Map(MigrationsSubscriptionStateV1.Cancelled, MigrationsSubscriptionState.Stopped));
}
```
