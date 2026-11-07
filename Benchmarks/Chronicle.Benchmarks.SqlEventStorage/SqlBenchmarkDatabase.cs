// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace Cratis.Chronicle.Benchmarks.SqlEventStorage;

/// <summary>Owns an isolated SQL database for a benchmark run.</summary>
public sealed class SqlBenchmarkDatabase : IAsyncDisposable
{
    IContainer? _container;

    /// <summary>Gets the host-side connection string.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Starts the selected database without logging credentials.</summary>
    /// <param name="provider">The SQL provider.</param>
    /// <returns>The startup task.</returns>
    public async Task Start(SqlStorageProvider provider)
    {
        var password = $"Benchmark-{Guid.NewGuid():N}!";
        if (provider == SqlStorageProvider.PostgreSql)
        {
            var container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("chronicle")
                .WithPassword(password)
                .WithLogger(NullLogger.Instance)
                .Build();
            _container = container;
            await container.StartAsync();
            ConnectionString = container.GetConnectionString();
            return;
        }

        var sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-CU10-ubuntu-24.04")
            .WithPassword(password)
            .WithLogger(NullLogger.Instance)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("SQL Server is now ready for client connections", wait => wait.WithTimeout(TimeSpan.FromSeconds(90))))
            .Build();
        _container = sql;
        await sql.StartAsync();
        ConnectionString = sql.GetConnectionString();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}
