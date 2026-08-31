```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

public enum MigrationsDotnetClientPaymentStatusV1
{
    Pending = 0,
    Settled = 1
}

public enum MigrationsDotnetClientPaymentStatus
{
    Awaiting = 10,
    Completed = 11
}

[EventType("dotnet-client-payment-processed", generation: 2)]
public record MigrationsDotnetClientPaymentProcessed(MigrationsDotnetClientPaymentStatus Status);

[EventTypeGenerationFor<MigrationsDotnetClientPaymentProcessed>(1)]
public record MigrationsDotnetClientPaymentProcessedV1(MigrationsDotnetClientPaymentStatusV1 Status);

public class MigrationsDotnetClientPaymentProcessedMigration : EventTypeMigration<MigrationsDotnetClientPaymentProcessed, MigrationsDotnetClientPaymentProcessedV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientPaymentProcessed, MigrationsDotnetClientPaymentProcessedV1> builder)
    {
        // Status is covered by the value map
    }

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientPaymentProcessedV1, MigrationsDotnetClientPaymentProcessed> builder)
    {
        // Status is covered by the value map
    }

    public override void MapValues(IEventValueMapBuilder<MigrationsDotnetClientPaymentProcessed, MigrationsDotnetClientPaymentProcessedV1> builder) =>
        builder.For(current => current.Status, previous => previous.Status, map => map
            .Map(MigrationsDotnetClientPaymentStatusV1.Pending, MigrationsDotnetClientPaymentStatus.Awaiting)
            .Map(MigrationsDotnetClientPaymentStatusV1.Settled, MigrationsDotnetClientPaymentStatus.Completed));
}
```
