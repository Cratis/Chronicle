// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_upgrading_a_sequence_without_an_event_source_column : given.a_named_tag_migrator
{
    bool _eventSourceColumnExists;
    bool _oldEventExists;

    async Task Establish()
    {
        await Execute($"CREATE TABLE \"event-sequence\" (SequenceNumber INTEGER PRIMARY KEY, Tags {(_provider == "SQLServer" ? "NVARCHAR(MAX)" : "TEXT")} NOT NULL)");
        await Execute("INSERT INTO \"event-sequence\" (SequenceNumber, Tags) VALUES (1, '[\"historic\"]')");
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.EnsureTableExists();

        // Selecting the column fails when the upgrade did not add it.
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"event-sequence\" WHERE SequenceNumber=1 AND EventSource IS NULL";
        _oldEventExists = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
        _eventSourceColumnExists = _oldEventExists;
    }

    [Fact] void should_add_the_event_source_column() => _eventSourceColumnExists.ShouldBeTrue();
    [Fact] void should_leave_existing_events_without_an_event_source() => _oldEventExists.ShouldBeTrue();
}
