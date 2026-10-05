// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Kernel.Integration.Storage.Sql.Alerts;

#pragma warning disable CA2100 // Database identifiers are task-owned GUID names, not external input; SQL cannot parameterize identifiers.

/// <summary>
/// Provides real SQL Server infrastructure for integration-only incident parity specs.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    const string Password = "Chronicle_specs_4424!";
    IContainer _container;

    /// <summary>
    /// Creates an isolated database in the SQL Server container.
    /// </summary>
    /// <returns>The isolated connection string.</returns>
    public async Task<string> CreateDatabase()
    {
        var name = $"spec_{Guid.NewGuid():N}";
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"localhost,{_container.GetMappedPublicPort(1433)}",
            InitialCatalog = "master",
            UserID = "sa",
            Password = Password,
            TrustServerCertificate = true,
            Pooling = false
        };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{name}]";
        await command.ExecuteNonQueryAsync();
        builder.InitialCatalog = name;

        return builder.ConnectionString;
    }

    /// <summary>
    /// Drops a task-owned isolated database.
    /// </summary>
    /// <param name="connectionString">The connection string of the task-owned database.</param>
    /// <returns>Awaitable cleanup.</returns>
    public async Task DropDatabase(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var name = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]";
        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder(SqlServerContainerImage.Name)
            .WithEnvironment("ACCEPT_EULA", "Y").WithEnvironment("MSSQL_SA_PASSWORD", Password)
            .WithPortBinding(1433, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", Password, "-C", "-Q", "SELECT 1"))
            .Build();
        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
