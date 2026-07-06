```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType]
public record MigrationsCombineShippingAddressRecordedV1(string Street, string City);

[EventType("shipping-address-recorded", generation: 2)]
public record MigrationsCombineShippingAddressRecorded(string FormattedAddress);

public class MigrationsCombineShippingAddressRecordedMigration : EventTypeMigration<MigrationsCombineShippingAddressRecorded, MigrationsCombineShippingAddressRecordedV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsCombineShippingAddressRecorded, MigrationsCombineShippingAddressRecordedV1> builder) =>
        builder.Properties(pb => pb
            .Combine(m => m.FormattedAddress, PropertySeparator.Space, e => e.Street, e => e.City)); // Joins with space separator

    public override void Downcast(IEventMigrationBuilder<MigrationsCombineShippingAddressRecordedV1, MigrationsCombineShippingAddressRecorded> builder) =>
        builder.Properties(pb => pb
            .Split(m => m.Street, e => e.FormattedAddress, PropertySeparator.Space, SplitPartIndex.First)
            .Split(m => m.City, e => e.FormattedAddress, PropertySeparator.Space, SplitPartIndex.Second));
}
```
