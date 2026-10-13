// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

#pragma warning disable CA2100 // Catalog queries use bound values; only trusted catalog identifiers are delimited below.
/// <summary>
/// Reads database and schema metadata without provisioning or migrating candidates.
/// </summary>
/// <param name="connectionString">The configured cluster connection.</param>
internal sealed class SqlDatabaseCatalog(string connectionString)
{
    /// <summary>
    /// Gets the SQL provider.
    /// </summary>
    internal DatabaseType Provider { get; } = connectionString.GetDatabaseType();

    /// <summary>
    /// Gets the configured, unchanged cluster database name.
    /// </summary>
    internal string ClusterName => Provider == DatabaseType.PostgreSql
        ? new NpgsqlConnectionStringBuilder(connectionString).Database!
        : new SqlConnectionStringBuilder(connectionString).InitialCatalog;

    /// <summary>
    /// Selects a physical database while keeping the remaining connection settings.
    /// </summary>
    /// <param name="name">The physical name.</param>
    /// <returns>The connection string.</returns>
    internal string ConnectionString(string name) => Provider == DatabaseType.PostgreSql
        ? new NpgsqlConnectionStringBuilder(connectionString) { Database = name }.ConnectionString
        : new SqlConnectionStringBuilder(connectionString) { InitialCatalog = name }.ConnectionString;

    /// <summary>
    /// Tests whether a candidate database already exists.
    /// </summary>
    /// <param name="name">The physical name.</param>
    /// <returns>Whether it exists.</returns>
    internal async Task<bool> Exists(string name)
    {
        await using var connection = CreateConnection(Provider == DatabaseType.PostgreSql ? "postgres" : "master");
        await connection.OpenAsync();
        var sql = Provider == DatabaseType.PostgreSql
            ? "SELECT COUNT(*) FROM pg_database WHERE datname::text COLLATE \"C\" = @name"
            : "SELECT COUNT(*) FROM sys.databases WHERE name = @name";
        await using var command = Command(connection, sql, ("name", name));
        return Convert.ToInt64(await command.ExecuteScalarAsync()) != 0;
    }

    /// <summary>
    /// Compares candidates using the provider's database-name equality.
    /// </summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns>Whether they name one database.</returns>
    internal async Task<bool> Equal(string left, string right)
    {
        if (Provider == DatabaseType.PostgreSql)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        await using var connection = CreateConnection("master");
        await connection.OpenAsync();

        // master uses the server's collation, including on servers with case-sensitive names.
        await using var command = Command(connection, "SELECT CASE WHEN @left COLLATE DATABASE_DEFAULT = @right COLLATE DATABASE_DEFAULT THEN 1 ELSE 0 END", ("left", left), ("right", right));
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    /// <summary>
    /// Reads user table names and their columns for ownership classification.
    /// </summary>
    /// <param name="name">The existing physical database.</param>
    /// <returns>Tables with their columns.</returns>
    internal async Task<IReadOnlyDictionary<string, string[]>> Tables(string name)
    {
        await using var connection = CreateConnection(name);
        await connection.OpenAsync();
        var sql = Provider == DatabaseType.PostgreSql
            ? "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public'"
            : "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo'";
        await using var command = Command(connection, sql);
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new List<(string Table, string Column)>();
        while (await reader.ReadAsync())
        {
            columns.Add((reader.GetString(0), reader.GetString(1)));
        }

        return columns.GroupBy(column => column.Table, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Select(column => column.Column).ToArray(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Reads schema ownership from an existing migration history table.
    /// </summary>
    /// <param name="name">The existing database.</param>
    /// <returns>The applied migrations.</returns>
    internal async Task<string[]> MigrationHistory(string name)
    {
        await using var connection = CreateConnection(name);
        await connection.OpenAsync();
        var sql = Provider == DatabaseType.PostgreSql
            ? "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\""
            : "SELECT [MigrationId] FROM [__EFMigrationsHistory]";
        await using var command = Command(connection, sql);
        await using var reader = await command.ExecuteReaderAsync();
        var migrations = new List<string>();
        while (await reader.ReadAsync())
        {
            migrations.Add(reader.GetString(0));
        }

        return migrations.ToArray();
    }

    static DbCommand Command(DbConnection connection, string sql, params (string Name, string Value)[] values)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in values)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    DbConnection CreateConnection(string name) => Provider == DatabaseType.PostgreSql ? new NpgsqlConnection(ConnectionString(name)) : new SqlConnection(ConnectionString(name));
}
