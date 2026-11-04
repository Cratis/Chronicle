// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_upgrading_an_existing_database;

/// <summary>
/// Upgrades an existing PostgreSQL database.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_a_multi_write_cycle_runs_on_it_on_postgresql(PostgreSqlFixture fixture) : and_a_multi_write_cycle_runs_on_it<PostgreSqlSinkHarness>
{
    /// <inheritdoc/>
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };
}
