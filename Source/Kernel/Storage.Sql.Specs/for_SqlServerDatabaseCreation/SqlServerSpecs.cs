// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.for_SqlServerDatabaseCreation;

#pragma warning disable CA2100 // Specification SQL built from trusted fixture names.
static class SqlServerSpecs
{
    internal static string Unpooled(string connectionString, string? catalog = null, string? application = null)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { Pooling = false };
        if (catalog is not null)
        {
            builder.InitialCatalog = catalog;
        }

        if (application is not null)
        {
            builder.ApplicationName = application;
        }

        return builder.ConnectionString;
    }

    internal static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(Unpooled(connectionString));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    internal static async Task<int> Scalar(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(Unpooled(connectionString, "master"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    internal static Task<int> SnapshotOn(string connectionString) =>
        Scalar(connectionString, $"SELECT CONVERT(int, is_read_committed_snapshot_on) FROM sys.databases WHERE name = '{new SqlConnectionStringBuilder(connectionString).InitialCatalog}'");
}
