// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

[Collection(SqlServerCollection.Name)]
public class and_the_event_has_several_writes_on_sql_server(SqlServerFixture fixture) : and_the_event_has_several_writes<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };
}
