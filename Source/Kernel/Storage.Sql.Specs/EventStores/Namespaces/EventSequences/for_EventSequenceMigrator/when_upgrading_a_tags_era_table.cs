// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_upgrading_a_tags_era_table : given.an_event_sequence_migrator
{
    const string TableName = "ExistingEventLog";

    string _preservedCorrelationId;
    string _preservedTags;
    string _existingRevisions;
    long _existingLastMutationOrdinal;
    string _oldStyleRevisions;
    long _oldStyleLastMutationOrdinal;
    int _mutationIndexCount;

    async Task Establish()
    {
        await Execute($$"""
            CREATE TABLE "{{TableName}}" (
                "SequenceNumber" INTEGER NOT NULL CONSTRAINT "PK_{{TableName}}" PRIMARY KEY,
                "CorrelationId" TEXT NOT NULL,
                "Causation" TEXT NOT NULL,
                "CausedBy" TEXT NOT NULL,
                "Type" TEXT NOT NULL,
                "Occurred" TEXT NOT NULL,
                "EventSourceType" TEXT NOT NULL,
                "EventSourceId" TEXT NOT NULL,
                "EventStreamType" TEXT NOT NULL,
                "EventStreamId" TEXT NOT NULL,
                "Content" TEXT NOT NULL,
                "ContentHashes" TEXT NOT NULL,
                "Compensations" TEXT NOT NULL,
                "Subject" TEXT NULL,
                "Tags" TEXT NOT NULL
            );
            INSERT INTO "{{TableName}}" (
                "SequenceNumber", "CorrelationId", "Causation", "CausedBy", "Type", "Occurred",
                "EventSourceType", "EventSourceId", "EventStreamType", "EventStreamId", "Content",
                "ContentHashes", "Compensations", "Subject", "Tags")
            VALUES (1, 'preserved-correlation', '[]', '[]', 'type', '2026-01-01T00:00:00Z',
                'source-type', 'source-id', 'stream-type', 'stream-id', '{}', '{}', '{}', NULL, '["preserved"]');
            """);
    }

    async Task Because()
    {
        await using (var context = CreateContext(TableName))
        {
            await context.EnsureTableExists();
        }

        _migrator.ClearMigrationCache(ConnectionString);
        await using (var context = CreateContext(TableName))
        {
            await context.EnsureTableExists();
        }

        await Execute($$"""
            INSERT INTO "{{TableName}}" (
                "SequenceNumber", "CorrelationId", "Causation", "CausedBy", "Type", "Occurred",
                "EventSourceType", "EventSourceId", "EventStreamType", "EventStreamId", "Content",
                "ContentHashes", "Compensations", "Subject", "Tags")
            VALUES (2, 'old-style', '[]', '[]', 'type', '2026-01-02T00:00:00Z',
                'source-type', 'source-id', 'stream-type', 'stream-id', '{}', '{}', '{}', NULL, '[]');
            """);

        await using (var command = _connection.CreateCommand())
        {
            command.CommandText = $"SELECT \"CorrelationId\", \"Tags\", \"Revisions\", \"LastMutationOrdinal\" FROM \"{TableName}\" WHERE \"SequenceNumber\" = 1";
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            _preservedCorrelationId = reader.GetString(0);
            _preservedTags = reader.GetString(1);
            _existingRevisions = reader.GetString(2);
            _existingLastMutationOrdinal = reader.GetInt64(3);
        }

        await using (var command = _connection.CreateCommand())
        {
            command.CommandText = $"SELECT \"Revisions\", \"LastMutationOrdinal\" FROM \"{TableName}\" WHERE \"SequenceNumber\" = 2";
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            _oldStyleRevisions = reader.GetString(0);
            _oldStyleLastMutationOrdinal = reader.GetInt64(1);
        }

        await using (var command = _connection.CreateCommand())
        {
            command.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_{TableName}_LastMutationOrdinal_SequenceNumber'";
            _mutationIndexCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        }
    }

    [Fact] void should_preserve_the_existing_row() => _preservedCorrelationId.ShouldEqual("preserved-correlation");
    [Fact] void should_preserve_existing_tags() => _preservedTags.ShouldEqual("[\"preserved\"]");
    [Fact] void should_backfill_empty_revisions() => _existingRevisions.ShouldEqual(string.Empty);
    [Fact] void should_backfill_zero_last_mutation_ordinal() => _existingLastMutationOrdinal.ShouldEqual(0L);
    [Fact] void should_allow_old_style_inserts_to_omit_revisions() => _oldStyleRevisions.ShouldEqual(string.Empty);
    [Fact] void should_allow_old_style_inserts_to_omit_last_mutation_ordinal() => _oldStyleLastMutationOrdinal.ShouldEqual(0L);
    [Fact] void should_create_the_mutation_index_once() => _mutationIndexCount.ShouldEqual(1);
}
