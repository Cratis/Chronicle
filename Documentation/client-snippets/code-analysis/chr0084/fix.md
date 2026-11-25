```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType("chr0084-fix-person-registered", generation: 2)]
public record Chr0084FixPersonRegistered(string FirstName, string LastName);

[EventTypeGenerationFor<Chr0084FixPersonRegistered>(1)]
public record Chr0084FixPersonRegisteredV1(string Name);

public class Chr0084FixPersonRegisteredV1ToChr0084FixPersonRegisteredMigration
    : EventTypeMigration<Chr0084FixPersonRegistered, Chr0084FixPersonRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<Chr0084FixPersonRegistered, Chr0084FixPersonRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .Split(m => m.FirstName, e => e.Name, PropertySeparator.Space, SplitPartIndex.First)
            .Split(m => m.LastName, e => e.Name, PropertySeparator.Space, SplitPartIndex.Second));

    public override void Downcast(IEventMigrationBuilder<Chr0084FixPersonRegisteredV1, Chr0084FixPersonRegistered> builder) =>
        builder.Properties(pb => pb
            .Combine(m => m.Name, PropertySeparator.Space, e => e.FirstName, e => e.LastName));
}
```
