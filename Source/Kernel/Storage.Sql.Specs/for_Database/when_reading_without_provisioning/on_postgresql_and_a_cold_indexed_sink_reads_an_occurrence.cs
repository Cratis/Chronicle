// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql_and_a_cold_indexed_sink_reads_an_occurrence(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    ISink _sink;
    string[] _databasesBefore;
    string[] _databasesAfter;
    int _bindingsAfter;

    async Task Establish()
    {
        await using var cluster = await Database.Cluster();
        _databasesBefore = await PhysicalDatabases();
    }

    async Task Because()
    {
        _sink = await given.ColdIndexedSink.ReadAnOccurrence(Database);
        _databasesAfter = await PhysicalDatabases();
        _bindingsAfter = await BindingCount();
    }

    [Fact] void should_resolve_the_sink() => _sink.ShouldNotBeNull();
    [Fact] void should_not_create_a_database() => string.Join(',', _databasesAfter).ShouldEqual(string.Join(',', _databasesBefore));
    [Fact] void should_not_bind_a_database_name() => _bindingsAfter.ShouldEqual(0);
}
