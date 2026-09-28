// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Provides a PostgreSQL database for SQL sink integration specs.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    const int Port = 5432;
    IContainer _container;

    /// <summary>
    /// Gets the connection string for the container.
    /// </summary>
    public string ConnectionString => $"Host=localhost;Port={_container.GetMappedPublicPort(Port)};Database=chronicle;Username=postgres;Password=postgres";

    /// <summary>
    /// Creates a database of its own in the container, for a specification that must not share state.
    /// </summary>
    /// <returns>The connection string for the new database.</returns>
    public async Task<string> CreateDatabase()
    {
        var name = $"spec_{Guid.NewGuid():N}";
        await Execute($"CREATE DATABASE \"{name}\"");
        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = name }.ToString();
    }

    /// <summary>
    /// Drops a database created by <see cref="CreateDatabase"/>, closing any connection still open to it.
    /// </summary>
    /// <param name="connectionString">The connection string <see cref="CreateDatabase"/> returned.</param>
    /// <returns>Awaitable task.</returns>
    public async Task DropDatabase(string connectionString)
    {
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            NpgsqlConnection.ClearPool(connection);
        }

        var name = new NpgsqlConnectionStringBuilder(connectionString).Database;
        await Execute($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // The module checks pg_isready over TCP, which is unavailable on the temporary init server.
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithPassword("postgres")
            .WithDatabase("chronicle")
            .Build();
        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    async Task Execute(string sql)
    {
        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }
}
