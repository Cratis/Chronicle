// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

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

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder("postgres:16-alpine")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithEnvironment("POSTGRES_DB", "chronicle")
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready", "-U", "postgres", "-d", "chronicle"))
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
}
