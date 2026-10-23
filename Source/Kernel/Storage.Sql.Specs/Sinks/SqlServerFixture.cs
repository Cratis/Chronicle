// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Provides isolated SQL Server databases for replay promotion specs.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    const int Port = 1433;
    const string Password = "Chronicle4476!Test";
    IContainer _container;

    string ConnectionString => $"Server=127.0.0.1,{_container.GetMappedPublicPort(Port)};Database=master;User Id=sa;Password={Password};TrustServerCertificate=true";

    /// <summary>
    /// Creates a database owned by one specification.
    /// </summary>
    /// <returns>The database connection string.</returns>
    public async Task<string> CreateDatabase()
    {
        var name = $"spec_{Guid.NewGuid():N}";
        await Execute($"CREATE DATABASE [{name}]");
        return new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = name }.ConnectionString;
    }

    /// <summary>
    /// Drops a specification's database after closing its pooled connections.
    /// </summary>
    /// <param name="connectionString">The connection string returned by <see cref="CreateDatabase"/>.</param>
    /// <returns>Awaitable task.</returns>
    public async Task DropDatabase(string connectionString)
    {
        await using (var connection = new SqlConnection(connectionString))
        {
            SqlConnection.ClearPool(connection);
        }

        var name = new SqlConnectionStringBuilder(connectionString).InitialCatalog.Replace("]", "]]", StringComparison.Ordinal);
        await Execute($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", Password)
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                ["/opt/mssql-tools18/bin/sqlcmd", "-C", "-S", "tcp:127.0.0.1,1433", "-U", "sa", "-P", Password, "-l", "3", "-Q", "SELECT 1"],
                wait => wait.WithTimeout(TimeSpan.FromSeconds(90))))
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
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Only fixture-owned database identifiers, delimited and escaped by the callers.
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }
}
