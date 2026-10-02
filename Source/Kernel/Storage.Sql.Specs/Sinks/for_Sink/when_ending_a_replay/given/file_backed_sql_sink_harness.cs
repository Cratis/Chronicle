// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay.given;

public class file_backed_sql_sink_harness : SqlSinkHarness
{
    readonly string _path = Path.Combine(Path.GetTempPath(), $"chronicle-replay-{Guid.NewGuid():N}.db");

    public file_backed_sql_sink_harness()
    {
        ConnectionString = new SqliteConnectionStringBuilder { DataSource = _path, Pooling = false }.ToString();
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();

        // WAL lets a second connection read the committed schema while promotion is paused mid-transaction.
        command.CommandText = "PRAGMA journal_mode=WAL";
        command.ExecuteNonQuery();
    }

    public override void Dispose()
    {
        base.Dispose();
        File.Delete(_path);
    }
}
