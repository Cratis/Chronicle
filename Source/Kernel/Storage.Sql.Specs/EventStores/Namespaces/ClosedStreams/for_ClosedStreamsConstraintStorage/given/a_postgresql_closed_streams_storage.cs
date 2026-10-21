// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage.given;

public class a_postgresql_closed_streams_storage(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    protected ClosedStreamsConstraintStorage _storage;

    async Task Establish()
    {
        await SeedStore(Store);
        _storage = new(Store, Namespace, EventSequenceId.Log, Database);
    }
}
