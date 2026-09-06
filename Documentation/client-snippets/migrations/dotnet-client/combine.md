```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType("dotnet-client-shipping-address-recorded", generation: 2)]
public record MigrationsDotnetClientCombineShippingAddressRecorded(string FullAddress);

[EventTypeGenerationFor<MigrationsDotnetClientCombineShippingAddressRecorded>(1)]
public record MigrationsDotnetClientCombineShippingAddressRecordedV1(string Street, string City);

public class MigrationsDotnetClientCombineShippingAddressRecordedMigration : EventTypeMigration<MigrationsDotnetClientCombineShippingAddressRecorded, MigrationsDotnetClientCombineShippingAddressRecordedV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientCombineShippingAddressRecorded, MigrationsDotnetClientCombineShippingAddressRecordedV1> builder) =>
        builder.Properties(pb => pb
            .Combine(t => t.FullAddress, PropertySeparator.Space, s => s.Street, s => s.City));

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientCombineShippingAddressRecordedV1, MigrationsDotnetClientCombineShippingAddressRecorded> builder) =>
        builder.Properties(pb => pb
            .Split(t => t.Street, s => s.FullAddress, PropertySeparator.Space, SplitPartIndex.First)
            .Split(t => t.City, s => s.FullAddress, PropertySeparator.Space, SplitPartIndex.Second));
}
```
