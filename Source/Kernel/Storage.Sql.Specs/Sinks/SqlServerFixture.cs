// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Storage.Sql.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Provides isolated SQL Server databases for replay promotion specs.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    const int Port = 1433;
    const int DatabaseInUse = 3702;
    const string Password = "Chronicle4476!Test";
#pragma warning disable CA2213 // DisposeAsync disposes the container through Timings.Run, which the analyzer cannot follow.
    IContainer _container;
#pragma warning restore CA2213
    SqlCommandTimings? _commandTimings;

    internal IntegrationTimings Timings { get; } = new(nameof(SqlServerFixture), "sql-specs", "mssql");

    internal T Track<T>(T context)
        where T : DbContext
    {
        if (!IntegrationTimings.Enabled) return context;
        if (_commandTimings is null)
        {
            _commandTimings = new(Timings);
            _commandTimings.Start();
        }
        return _commandTimings.Track(context);
    }

    string ConnectionString => $"Server=127.0.0.1,{_container.GetMappedPublicPort(Port)};Database=master;User Id=sa;Password={Password};TrustServerCertificate=true";

    /// <summary>
    /// Creates a database owned by one specification.
    /// </summary>
    /// <returns>The database connection string.</returns>
    public async Task<string> CreateDatabase()
    {
        var name = $"spec_{Guid.NewGuid():N}";
        await Timings.Run("sql-database-create", () => Execute($"CREATE DATABASE [{name}]"));
        return new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = name }.ConnectionString;
    }

    /// <summary>
    /// Drops a specification's database after closing its pooled connections, falling back to evicting open connections.
    /// </summary>
    /// <param name="connectionString">The connection string returned by <see cref="CreateDatabase"/>.</param>
    /// <returns>Awaitable task.</returns>
    public async Task DropDatabase(string connectionString)
    {
        // Specs open the database through differently configured connection strings, so clearing one pool is not enough.
        SqlConnection.ClearAllPools();

        var name = new SqlConnectionStringBuilder(connectionString).InitialCatalog.Replace("]", "]]", StringComparison.Ordinal);
        await Timings.Run("sql-database-drop", async () =>
        {
            try
            {
                await Execute($"DROP DATABASE [{name}]");
            }
            catch (SqlException ex) when (ex.Number == DatabaseInUse)
            {
                // A connection outside the pool is still open; SINGLE_USER ... ROLLBACK IMMEDIATE evicts it, at a cost of seconds.
                await Execute($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");
            }
        });
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new ContainerBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", Password)
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                ["/opt/mssql-tools18/bin/sqlcmd", "-C", "-S", "tcp:127.0.0.1,1433", "-U", "sa", "-P", Password, "-l", "3", "-Q", "SELECT 1"],
                wait => wait.WithTimeout(TimeSpan.FromSeconds(90))))
            .Build();
        await Timings.Run("sql-container-start", () => _container.StartAsync());
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_container is not null)
            {
                await Timings.Run("sql-container-disposal", () => _container.DisposeAsync().AsTask());
            }
        }
        finally
        {
            _commandTimings?.Dispose();
            Timings.Complete();
        }
    }

    async Task Execute(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Only fixture-owned database identifiers, delimited and escaped by the callers.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }
}
