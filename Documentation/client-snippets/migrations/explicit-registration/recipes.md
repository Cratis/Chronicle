```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace ExplicitMigrationExample
{
    public record Previous(string OldName);
    public record Current(string NewName);

    public class PlainRename
    {
        public void Upcast(IEventMigrationBuilder<Current, Previous> builder) =>
            builder.Properties(_ => _.RenamedFrom(current => current.NewName, previous => previous.OldName));

        public void Downcast(IEventMigrationBuilder<Previous, Current> builder) =>
            builder.Properties(_ => _.RenamedFrom(previous => previous.OldName, current => current.NewName));
    }

    public class TypedRename(IEventTypes eventTypes) : EventTypeMigration<Current, Previous>(eventTypes)
    {
        public override void Upcast(IEventMigrationBuilder<Current, Previous> builder) =>
            builder.Properties(_ => _.RenamedFrom(current => current.NewName, previous => previous.OldName));

        public override void Downcast(IEventMigrationBuilder<Previous, Current> builder) =>
            builder.Properties(_ => _.RenamedFrom(previous => previous.OldName, current => current.NewName));
    }

    public static class Composition
    {
        public static ChronicleOptions CreateOptions()
        {
            var options = new ChronicleOptions();
            options.ExplicitArtifacts
                .RegisterEventType<Previous>(_ => _.WithId("person-registered").Generation(1))
                .RegisterEventType<Current>(_ => _.WithId("person-registered").Generation(2))
                .RegisterMigration<PlainRename>();
            // Register TypedRename instead to use the registry-taking base constructor.
            return options;
        }
    }
}
```
