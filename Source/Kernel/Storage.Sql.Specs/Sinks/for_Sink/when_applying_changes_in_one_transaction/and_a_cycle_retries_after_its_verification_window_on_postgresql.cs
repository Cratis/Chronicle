// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

[Collection(PostgreSqlCollection.Name)]
public class and_a_cycle_retries_after_its_verification_window_on_postgresql(PostgreSqlFixture fixture) : and_a_cycle_retries_after_its_verification_window<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture], ConfigureOptions = given.retrying_on_timeout.Configure };
}
