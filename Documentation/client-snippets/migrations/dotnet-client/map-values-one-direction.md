```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

public enum MigrationsDotnetClientDeclineReasonV1
{
    InsufficientFunds = 0,
    CardExpired = 1,
    CardReported = 2
}

public enum MigrationsDotnetClientDeclineReason
{
    Funds = 0,
    Card = 1
}

[EventType("dotnet-client-payment-declined", generation: 2)]
public record MigrationsDotnetClientPaymentDeclined(MigrationsDotnetClientDeclineReason Reason);

[EventTypeGenerationFor<MigrationsDotnetClientPaymentDeclined>(1)]
public record MigrationsDotnetClientPaymentDeclinedV1(MigrationsDotnetClientDeclineReasonV1 Reason);

public class MigrationsDotnetClientPaymentDeclinedMigration : EventTypeMigration<MigrationsDotnetClientPaymentDeclined, MigrationsDotnetClientPaymentDeclinedV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientPaymentDeclined, MigrationsDotnetClientPaymentDeclinedV1> builder) =>
        builder.Properties(pb => pb
            .MapValues(current => current.Reason, previous => previous.Reason, map => map
                .Map(MigrationsDotnetClientDeclineReasonV1.InsufficientFunds, MigrationsDotnetClientDeclineReason.Funds)
                .Map(MigrationsDotnetClientDeclineReasonV1.CardExpired, MigrationsDotnetClientDeclineReason.Card)
                .Map(MigrationsDotnetClientDeclineReasonV1.CardReported, MigrationsDotnetClientDeclineReason.Card)));

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientPaymentDeclinedV1, MigrationsDotnetClientPaymentDeclined> builder) =>
        builder.Properties(pb => pb
            .MapValues(previous => previous.Reason, current => current.Reason, map => map
                .Map(MigrationsDotnetClientDeclineReason.Funds, MigrationsDotnetClientDeclineReasonV1.InsufficientFunds)
                .Map(MigrationsDotnetClientDeclineReason.Card, MigrationsDotnetClientDeclineReasonV1.CardExpired)));
}
```
