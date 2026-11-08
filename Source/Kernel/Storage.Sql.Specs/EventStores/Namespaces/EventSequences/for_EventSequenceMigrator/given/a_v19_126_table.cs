// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator.given;

public class a_v19_126_table : a_named_tag_migrator
{
    async Task Establish()
    {
        await Execute("""
            CREATE TABLE "event-sequence" (
                SequenceNumber BIGINT PRIMARY KEY, CorrelationId VARCHAR(1000), Causation VARCHAR(1000), CausedBy VARCHAR(1000),
                Type VARCHAR(200), AppendedGeneration INTEGER, RevisedGeneration INTEGER, Occurred VARCHAR(100),
                EventSourceType VARCHAR(200), EventSourceId VARCHAR(200), EventStreamType VARCHAR(200), EventStreamId VARCHAR(200),
                Content VARCHAR(4000), ContentHashes VARCHAR(4000), DerivedGenerations VARCHAR(4000), Compensations VARCHAR(4000),
                Subject VARCHAR(1000), Tags VARCHAR(4000), EventSource VARCHAR(1000), CompensationFor BIGINT,
                PublicationIdentityHash VARCHAR(32), PublicationId VARCHAR(1000), PublicationFingerprint VARCHAR(1000))
            """);
        await Execute("""INSERT INTO "event-sequence" (SequenceNumber, Type, Content, ContentHashes) VALUES (1, 'legacy', '{"1":{"value":"original"}}', '{"1":"original-hash"}')""");
    }

    protected async Task<int> PreservedRows()
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = """SELECT COUNT(*) FROM "event-sequence" WHERE SequenceNumber=1 AND Content='{"1":{"value":"original"}}' AND ContentHashes='{"1":"original-hash"}' AND OriginalContent IS NULL AND Revisions IS NULL""";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
