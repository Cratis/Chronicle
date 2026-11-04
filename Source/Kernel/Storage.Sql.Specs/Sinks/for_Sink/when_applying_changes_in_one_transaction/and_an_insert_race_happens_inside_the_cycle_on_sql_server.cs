// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

[Collection(SqlServerCollection.Name)]
public class and_an_insert_race_happens_inside_the_cycle_on_sql_server(SqlServerFixture fixture) : and_an_insert_race_happens_inside_the_cycle<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture], ConfigureOptions = given.retrying_on_timeout.Configure };
}
