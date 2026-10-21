// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_migrating_concurrently;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    (string[][] Pending, int Marker) _result;

    async Task Because() => _result = await given.migration_race.Run(CreateDatabase, new SqlConnection(ConnectionString));

    [Fact] void should_finish_migrations_for_every_instance() => _result.Pending.Length.ShouldEqual(4);
    [Fact] void should_leave_no_pending_migrations() => _result.Pending.SelectMany(_ => _).ShouldBeEmpty();
    [Fact] void should_preserve_unrelated_data() => _result.Marker.ShouldEqual(42);
}
