// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_SqlServerDatabaseCreation.when_ensuring_an_existing_database;

[Collection(SqlServerCollection.Name)]
public class and_it_is_empty_without_read_committed_snapshot(SqlServerFixture fixture) : Specification
{
    string _connectionString;
    int _snapshotOn;

    async Task Establish() => _connectionString = await fixture.CreateDatabase();

    async Task Because()
    {
        await SqlServerReadCommittedSnapshot.WarnIfOff(_connectionString, null, NullLogger.Instance);
        _snapshotOn = await SqlServerSpecs.SnapshotOn(_connectionString);
    }

    async Task Destroy() => await fixture.DropDatabase(_connectionString);

    [Fact] void should_leave_existing_database_unchanged() => _snapshotOn.ShouldEqual(0);
}
