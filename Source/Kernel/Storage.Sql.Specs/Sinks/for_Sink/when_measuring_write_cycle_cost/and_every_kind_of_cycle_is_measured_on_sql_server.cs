// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_measuring_write_cycle_cost;

/// <summary>
/// Measures the cost of each kind of write cycle on SQL Server.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
[Collection(SqlServerCollection.Name)]
public class and_every_kind_of_cycle_is_measured_on_sql_server(SqlServerFixture fixture) : and_every_kind_of_cycle_is_measured<SqlServerSinkHarness>
{
    /// <inheritdoc/>
    protected override string Database => "sqlserver";

    /// <inheritdoc/>
    protected override SqlServerSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };
}
