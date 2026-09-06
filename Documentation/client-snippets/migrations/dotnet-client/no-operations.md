```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

public sealed record MigrationsDotnetClientOptionalNote(string Text, string? Author = null);

public sealed record MigrationsDotnetClientOptionalNoteV1(string Text);

[EventType("dotnet-client-optional-note-added", generation: 2)]
public record MigrationsDotnetClientNoteAdded(MigrationsDotnetClientOptionalNote Note);

[EventTypeGenerationFor<MigrationsDotnetClientNoteAdded>(1)]
public record MigrationsDotnetClientNoteAddedV1(MigrationsDotnetClientOptionalNoteV1 Note);

public class MigrationsDotnetClientNoteAddedMigration : EventTypeMigration<MigrationsDotnetClientNoteAdded, MigrationsDotnetClientNoteAddedV1>
{
    // Author is optional, so the target generation's schema fills it in on the way up
    // and drops it on the way down. The class only has to exist.
    public override void Upcast(IEventMigrationBuilder<MigrationsDotnetClientNoteAdded, MigrationsDotnetClientNoteAddedV1> builder)
    {
    }

    public override void Downcast(IEventMigrationBuilder<MigrationsDotnetClientNoteAddedV1, MigrationsDotnetClientNoteAdded> builder)
    {
    }
}
```
