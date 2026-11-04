// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_upgrading_an_existing_database;

/// <summary>
/// Upgrades an existing SQLite database, which is a file when CHRONICLE_SINK_PROOF_SQLITE is set.
/// </summary>
public class and_a_multi_write_cycle_runs_on_it_on_sqlite : and_a_multi_write_cycle_runs_on_it<SqlSinkHarness>
{
    /// <inheritdoc/>
    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture)
    {
        var file = Environment.GetEnvironmentVariable("CHRONICLE_SINK_PROOF_SQLITE");
        if (string.IsNullOrEmpty(file))
        {
            return new() { Interceptors = [capture] };
        }

        return new()
        {
            ConnectionString = new SqliteConnectionStringBuilder { DataSource = file, Cache = SqliteCacheMode.Private, Pooling = false }.ToString(),
            Interceptors = [capture]
        };
    }
}
