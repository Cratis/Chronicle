// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

/// <summary>
/// Both new and previously migrated sequences need the bounded parent-history access path.
/// </summary>
public class when_ensuring_the_parent_history_index : given.a_named_tag_migrator
{
    const string IndexName = "IX_event-sequence_parentHistory";
    string[] _newSequenceColumns;
    string[] _upgradedSequenceColumns;
    string[] _existingIndexColumns;

    async Task Establish()
    {
        await using var context = CreateContext();
        await context.EnsureTableExists();
        _newSequenceColumns = await ReadIndexColumns();
        await Execute(_provider == "SQLServer"
            ? $"DROP INDEX \"{IndexName}\" ON \"event-sequence\""
            : $"DROP INDEX \"{IndexName}\"");
    }

    async Task Because()
    {
        await EnsureAfterRestart();
        _upgradedSequenceColumns = await ReadIndexColumns();
        await EnsureAfterRestart();
        _existingIndexColumns = await ReadIndexColumns();
    }

    async Task EnsureAfterRestart()
    {
        await using var context = CreateContext();
        var migrator = new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance);
        migrator.ClearMigrationCacheForConnectionString(context.Database.GetConnectionString()!);
        await context.EnsureTableExists();
    }

    async Task<string[]> ReadIndexColumns()
    {
        await using var command = _connection.CreateCommand();
        switch (_provider)
        {
            case "PostgreSQL":
                command.CommandText = "SELECT a.attname FROM pg_class t JOIN pg_namespace n ON t.relnamespace = n.oid JOIN pg_index i ON i.indrelid = t.oid JOIN pg_class ix ON ix.oid = i.indexrelid JOIN LATERAL unnest(i.indkey) WITH ORDINALITY AS k(attnum, ordinal) ON true JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = k.attnum WHERE n.nspname = current_schema() AND t.relname = @table AND ix.relname = @index ORDER BY k.ordinal";
                break;
            case "SQLServer":
                command.CommandText = "SELECT c.name FROM sys.indexes i JOIN sys.tables t ON i.object_id = t.object_id JOIN sys.schemas s ON t.schema_id = s.schema_id JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id WHERE s.name = SCHEMA_NAME() AND t.name = @table AND i.name = @index ORDER BY ic.key_ordinal";
                break;
            default:
                command.CommandText = "SELECT name FROM pragma_index_info(@index) ORDER BY seqno";
                break;
        }
        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@table";
        tableParameter.Value = "event-sequence";
        command.Parameters.Add(tableParameter);
        var indexParameter = command.CreateParameter();
        indexParameter.ParameterName = "@index";
        indexParameter.Value = IndexName;
        command.Parameters.Add(indexParameter);
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }
        return columns.ToArray();
    }

    [Fact] void should_index_new_sequences_by_source_type_and_sequence() => _newSequenceColumns.ShouldEqual(["EventSourceId", "Type", "SequenceNumber"]);
    [Fact] void should_add_the_same_index_to_existing_sequences() => _upgradedSequenceColumns.ShouldEqual(_newSequenceColumns);
    [Fact] void should_leave_an_existing_index_intact_on_restart() => _existingIndexColumns.ShouldEqual(_newSequenceColumns);
}
