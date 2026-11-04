// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_measuring_write_cycle_cost;

/// <summary>
/// Measures the cost of each kind of write cycle on SQLite.
/// </summary>
public class and_every_kind_of_cycle_is_measured_on_sqlite : and_every_kind_of_cycle_is_measured<SqlSinkHarness>
{
    /// <inheritdoc/>
    protected override string Database => "sqlite";

    /// <inheritdoc/>
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture] };
}
