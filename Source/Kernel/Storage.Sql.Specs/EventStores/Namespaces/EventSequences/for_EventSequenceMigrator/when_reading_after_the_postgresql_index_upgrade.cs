// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

[Collection(PostgreSqlCollection.Name)]
public class when_reading_after_the_postgresql_index_upgrade(PostgreSqlFixture fixture)
{
    [Fact]
    async Task should_preserve_the_storage_read_corpus_and_duplicate_slot_handling()
    {
        var connectionString = await fixture.CreateDatabase();
        try
        {
            using var host = EventSequenceStorageHost.ForPostgreSql(connectionString, given.SequenceIndexReadCorpus.Identities(), Substitute.For<IEventTypesStorage>(), cacheServiceProvider: true);
            await given.SequenceIndexReadCorpus.Verify(host);
        }
        finally
        {
            await fixture.DropDatabase(connectionString);
        }
    }
}
