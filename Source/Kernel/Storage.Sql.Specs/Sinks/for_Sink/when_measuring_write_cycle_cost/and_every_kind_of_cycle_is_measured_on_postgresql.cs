// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_measuring_write_cycle_cost;

/// <summary>
/// Measures the cost of each kind of write cycle on PostgreSQL.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_every_kind_of_cycle_is_measured_on_postgresql(PostgreSqlFixture fixture) : and_every_kind_of_cycle_is_measured<PostgreSqlSinkHarness>
{
    /// <inheritdoc/>
    protected override string Database => "postgresql";

    /// <inheritdoc/>
    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };
}
