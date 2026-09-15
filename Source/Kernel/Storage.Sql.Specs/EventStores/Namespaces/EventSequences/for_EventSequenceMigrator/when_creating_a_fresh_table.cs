// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_creating_a_fresh_table : given.an_event_sequence_migrator
{
    const string TableName = "FreshEventLog";

    (string Type, bool NotNull, string? Default) _revisions;
    (string Type, bool NotNull, string? Default) _lastMutationOrdinal;
    string[] _indexColumns;

    async Task Because()
    {
        await using (var context = CreateContext(TableName))
        {
            await context.EnsureTableExists();
        }

        await using (var command = _connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA table_info(\"{TableName}\")";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(1);
                var column = (reader.GetString(2), reader.GetInt32(3) == 1, await reader.IsDBNullAsync(4) ? null : reader.GetString(4));
                if (name == nameof(EventEntry.Revisions))
                {
                    _revisions = column;
                }
                else if (name == nameof(EventEntry.LastMutationOrdinal))
                {
                    _lastMutationOrdinal = column;
                }
            }
        }

        var columns = new List<string>();
        await using (var command = _connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA index_info(\"IX_{TableName}_LastMutationOrdinal_SequenceNumber\")";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(2));
            }
        }
        _indexColumns = columns.ToArray();
    }

    [Fact] void should_create_revisions_as_required_text() => _revisions.Type.ShouldEqual("TEXT");
    [Fact] void should_make_revisions_not_nullable() => _revisions.NotNull.ShouldBeTrue();
    [Fact] void should_default_revisions_to_empty_text() => _revisions.Default.ShouldEqual("''");
    [Fact] void should_create_last_mutation_ordinal_as_required_integer() => _lastMutationOrdinal.Type.ShouldEqual("INTEGER");
    [Fact] void should_make_last_mutation_ordinal_not_nullable() => _lastMutationOrdinal.NotNull.ShouldBeTrue();
    [Fact] void should_default_last_mutation_ordinal_to_zero() => _lastMutationOrdinal.Default.ShouldEqual("0");
    [Fact] void should_index_last_mutation_ordinal_before_sequence_number() => _indexColumns.ShouldEqual([nameof(EventEntry.LastMutationOrdinal), nameof(EventEntry.SequenceNumber)]);
}
