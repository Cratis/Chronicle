// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

[Collection(PostgreSqlCollection.Name)]
public class and_a_cycle_is_retried_on_postgresql(PostgreSqlFixture fixture) : and_a_cycle_is_retried<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture], ConfigureOptions = Configure };
}
