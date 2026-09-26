// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_upgrading_a_sequence_with_tags : given.a_named_tag_migrator
{
    bool _companionExists;
    bool _oldEventExists;

    async Task Establish()
    {
        await Execute("CREATE TABLE \"event-sequence\" (SequenceNumber INTEGER PRIMARY KEY, Tags TEXT NOT NULL)");
        await Execute("INSERT INTO \"event-sequence\" (SequenceNumber, Tags) VALUES (1, '[\"historic\"]')");
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.EnsureTableExists();
        _companionExists = await Exists(EventSequenceMigrator.NamedTagsTable);
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"event-sequence\" WHERE SequenceNumber=1 AND Tags='[\"historic\"]'";
        _oldEventExists = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    [Fact] void should_create_companion_even_if_legacy_tags_column_exists() => _companionExists.ShouldBeTrue();
    [Fact] void should_leave_existing_events_unchanged() => _oldEventExists.ShouldBeTrue();
}
