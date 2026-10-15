// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Creates databases whose valid sysname would exceed the limit of SQL Server's default logical log filename.
/// </summary>
internal static class SqlServerDatabaseCreation
{
    /// <summary>
    /// Creates a database with bounded, database-specific logical and physical filenames.
    /// </summary>
    /// <param name="connectionString">The target connection string.</param>
    /// <returns>The creation operation.</returns>
    internal static async Task Create(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "DECLARE @dataPath nvarchar(260) = CONVERT(nvarchar(260), SERVERPROPERTY('InstanceDefaultDataPath')); " +
            "DECLARE @logPath nvarchar(260) = CONVERT(nvarchar(260), SERVERPROPERTY('InstanceDefaultLogPath')); " +
            "DECLARE @sql nvarchar(max); " +
            "IF @dataPath IS NULL OR @logPath IS NULL " +
            "    SET @sql = N'CREATE DATABASE ' + QUOTENAME(@database); " +
            "ELSE " +
            "    SET @sql = N'CREATE DATABASE ' + QUOTENAME(@database) " +
            "        + N' ON (NAME = ' + QUOTENAME(@file, '''') " +
            "        + N', FILENAME = N''' + REPLACE(@dataPath + @file + N'.mdf', N'''', N'''''') + N''')' " +
            "        + N' LOG ON (NAME = ' + QUOTENAME(@file + N'_log', '''') " +
            "        + N', FILENAME = N''' + REPLACE(@logPath + @file + N'.ldf', N'''', N'''''') + N''')'; " +
            "EXEC sp_executesql @sql; " +
            "IF SERVERPROPERTY('EngineEdition') <> 5 " +
            "BEGIN " +
            "    SET @sql = N'ALTER DATABASE ' + QUOTENAME(@database) + N' SET READ_COMMITTED_SNAPSHOT ON'; " +
            "    EXEC sp_executesql @sql; " +
            "END;";
        command.Parameters.AddWithValue("database", database);
        var file = "chronicle_" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(database)));
        command.Parameters.AddWithValue("file", file);
        await command.ExecuteNonQueryAsync();

        // A failed open before creation leaves the pool in its blocking period, so the next open of the new
        // database would fail as if it did not exist. EF Core's own creator clears the pools for the same reason.
        SqlConnection.ClearAllPools();
    }
}
