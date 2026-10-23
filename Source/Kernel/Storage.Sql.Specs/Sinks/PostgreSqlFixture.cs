// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Provides a PostgreSQL database for SQL sink integration specs.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    const int Port = 5432;
    readonly ConcurrentDictionary<Type, Lazy<Task<string>>> _templates = new();
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
    /// Creates an isolated database from a template migrated once for the context type.
    /// </summary>
    /// <typeparam name="TContext">The context type, constructed with its typed database options.</typeparam>
    /// <returns>The connection string for the new database.</returns>
    public async Task<string> CreateMigratedDatabase<TContext>()
        where TContext : DbContext
    {
        var template = await _templates.GetOrAdd(typeof(TContext), static (_, fixture) => new Lazy<Task<string>>(fixture.MigrateTemplate<TContext>), this).Value;
        var name = $"spec_{Guid.NewGuid():N}";
        await Execute($"CREATE DATABASE \"{name}\" TEMPLATE \"{template}\"");
        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = name }.ToString();
    }

    /// <summary>
    /// Drops a specification database, closing any connection still open to it.
    /// </summary>
    /// <param name="connectionString">The connection string returned when the database was created.</param>
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
        _container = new ContainerBuilder("postgres:16-alpine")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithEnvironment("POSTGRES_DB", "chronicle")
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-h", "127.0.0.1", "-U", "postgres", "-d", "chronicle"))
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

    async Task<string> MigrateTemplate<TContext>()
        where TContext : DbContext
    {
        var connectionString = await CreateDatabase();
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(connectionString)
            .AddConceptAsSupport().WithoutServiceProviderCaching()
            .Options;
        await using (var context = (TContext)Activator.CreateInstance(typeof(TContext), options)!)
        {
            await context.Database.MigrateAsync();
        }

        // Closing the context returns its connection to the pool; PostgreSQL cannot copy an open template.
        await using var connection = new NpgsqlConnection(connectionString);
        NpgsqlConnection.ClearPool(connection);
        return new NpgsqlConnectionStringBuilder(connectionString).Database!;
    }

    async Task Execute(string sql)
    {
        await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }
}
