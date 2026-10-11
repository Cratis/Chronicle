// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_upgrading_a_sequence_without_revised_generation : given.a_named_tag_migrator
{
    int _legacyRowsWithoutRevisionMetadata;

    async Task Establish()
    {
        await Execute("CREATE TABLE \"event-sequence\" (SequenceNumber BIGINT PRIMARY KEY)");
        await Execute("INSERT INTO \"event-sequence\" (SequenceNumber) VALUES (1)");
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await context.EnsureTableExists();
        await context.EnsureTableExists();
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"event-sequence\" WHERE SequenceNumber=1 AND RevisedGeneration IS NULL";
        _legacyRowsWithoutRevisionMetadata = Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    [Fact] void should_leave_legacy_revisions_unknown() => _legacyRowsWithoutRevisionMetadata.ShouldEqual(1);
}
