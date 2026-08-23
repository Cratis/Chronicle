```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType("payment-processed", generation: 2)]
public record MigrationsRenamePaymentProcessed(decimal Amount);

[EventTypeGenerationFor<MigrationsRenamePaymentProcessed>(1)]
public record MigrationsRenamePaymentProcessedV1(decimal OldAmount);

public class MigrationsRenamePaymentProcessedMigration : EventTypeMigration<MigrationsRenamePaymentProcessed, MigrationsRenamePaymentProcessedV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsRenamePaymentProcessed, MigrationsRenamePaymentProcessedV1> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(m => m.Amount, e => e.OldAmount));

    public override void Downcast(IEventMigrationBuilder<MigrationsRenamePaymentProcessedV1, MigrationsRenamePaymentProcessed> builder) =>
        builder.Properties(pb => pb
            .RenamedFrom(m => m.OldAmount, e => e.Amount));
}
```
