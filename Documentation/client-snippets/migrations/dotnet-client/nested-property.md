```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

public sealed record MigrationsDotnetClientNestedPrice(decimal Amount, string Description);

public sealed record MigrationsDotnetClientNestedPriceV1(decimal Amount);

[EventType("dotnet-client-nested-price-set", generation: 2)]
public record MigrationsDotnetClientNestedPriceSet(MigrationsDotnetClientNestedPrice Price);

[EventTypeGenerationFor<MigrationsDotnetClientNestedPriceSet>(1)]
public record MigrationsDotnetClientNestedPriceSetV1(MigrationsDotnetClientNestedPriceV1 Price);

public class MigrationsDotnetClientNestedPriceSetMigration : EventTypeMigration<MigrationsDotnetClientNestedPriceSet, MigrationsDotnetClientNestedPriceSetV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientNestedPriceSet, MigrationsDotnetClientNestedPriceSetV1> builder) =>
        builder.Properties(pb => pb.DefaultValue(m => m.Price.Description, "unspecified"));

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientNestedPriceSetV1, MigrationsDotnetClientNestedPriceSet> builder)
    {
        // Description does not exist in generation 1 - the schema drops it on the way back
    }
}
```
