```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

[EventType("author-registered", generation: 2)]
public record MigrationsAuthorRegistered(string FirstName, string LastName);

[EventTypeGenerationFor<MigrationsAuthorRegistered>(1)]
public record MigrationsAuthorRegisteredV1(string Name);

public class MigrationsAuthorRegisteredMigration : EventTypeMigration<MigrationsAuthorRegistered, MigrationsAuthorRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<MigrationsAuthorRegistered, MigrationsAuthorRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .Split(m => m.FirstName, e => e.Name, PropertySeparator.Space, SplitPartIndex.First)
            .Split(m => m.LastName, e => e.Name, PropertySeparator.Space, SplitPartIndex.Second));

    public override void Downcast(IEventMigrationBuilder<MigrationsAuthorRegisteredV1, MigrationsAuthorRegistered> builder) =>
        builder.Properties(pb => pb
            .Combine(m => m.Name, PropertySeparator.Space, e => e.FirstName, e => e.LastName));
}
```
