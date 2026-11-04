// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

[Collection(PostgreSqlCollection.Name)]
public class and_receipts_are_created_under_migrate_on_postgresql(PostgreSqlFixture fixture) : and_receipts_are_created_under_migrate<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture], ConfigureOptions = Configure };
}
