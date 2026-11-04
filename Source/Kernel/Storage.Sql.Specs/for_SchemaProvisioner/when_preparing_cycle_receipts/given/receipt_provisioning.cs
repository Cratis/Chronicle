// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_preparing_cycle_receipts.given;

/// <summary>
/// Explicit provisioning of the sink cycle receipts of the read-model database, inspected through a plain connection.
/// </summary>
public abstract class receipt_provisioning : for_SchemaProvisioner.given.a_schema_provisioner
{
    protected const string Table = "chronicle_sink_cycle_receipts";

    protected string ReadModelDatabase => Directory.GetFiles(_directory, "*.db").First(file => Scalar(file, $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{Table}'") == 1);

    protected static long Scalar(string file, string sql)
    {
        using var connection = Open(file);
        using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Specification-owned statements.
        command.CommandText = sql;
#pragma warning restore CA2100
        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    protected static void Execute(string file, string sql)
    {
        using var connection = Open(file);
        using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Specification-owned statements.
        command.CommandText = sql;
#pragma warning restore CA2100
        command.ExecuteNonQuery();
    }

    static SqliteConnection Open(string file)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Pooling = false }.ConnectionString);
        connection.Open();
        return connection;
    }
}
