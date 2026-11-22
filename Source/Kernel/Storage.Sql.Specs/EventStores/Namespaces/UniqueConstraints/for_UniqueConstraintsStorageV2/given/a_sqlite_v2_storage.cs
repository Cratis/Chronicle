// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2.given;

public abstract class a_sqlite_v2_storage : a_sql_v2_storage
{
    string _directory;

    protected override Task<string> CreateDatabase()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"chronicle-v2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        return Task.FromResult(new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "namespace.db"), Pooling = false, DefaultTimeout = 30 }.ConnectionString);
    }

    protected override DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString) => builder.UseSqlite(connectionString);

    protected override Task ReleaseDatabase()
    {
        Directory.Delete(_directory, recursive: true);
        return Task.CompletedTask;
    }

    protected async Task<string[]> Catalog()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name || ':' || sql FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' ORDER BY name";
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync()) rows.Add(reader.GetString(0));
        return rows.ToArray();
    }

    protected async Task Execute(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        await command.ExecuteNonQueryAsync();
    }
}
