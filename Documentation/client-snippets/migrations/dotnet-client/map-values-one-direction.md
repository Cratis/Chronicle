```csharp
public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientPaymentProcessed, MigrationsDotnetClientPaymentProcessedV1> builder) =>
    builder.Properties(pb => pb
        .MapValues(current => current.Status, previous => previous.Status, map => map
            .Map(MigrationsDotnetClientPaymentStatusV1.Pending, MigrationsDotnetClientPaymentStatus.Awaiting)
            .Map(MigrationsDotnetClientPaymentStatusV1.Settled, MigrationsDotnetClientPaymentStatus.Completed)));
```
