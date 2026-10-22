// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Associates EF migration attempts with the process and physical store that execute them.
/// </summary>
internal static partial class SqlMigrationTelemetry
{
    /// <summary>
    /// Routes EF migration-application events to structured logging without connection details.
    /// </summary>
    /// <param name="builder">The context options.</param>
    /// <param name="connectionString">The connection from which only the physical database name is extracted.</param>
    /// <param name="logger">The SQL database logger.</param>
    /// <typeparam name="TContext">The database context type.</typeparam>
    internal static void Configure<TContext>(DbContextOptionsBuilder<TContext> builder, string connectionString, ILogger logger)
        where TContext : DbContext
    {
        var store = connectionString.GetDatabaseType() switch
        {
            DatabaseType.SqlServer => new SqlConnectionStringBuilder(connectionString).InitialCatalog,
            DatabaseType.PostgreSql => new NpgsqlConnectionStringBuilder(connectionString).Database ?? string.Empty,
            _ => Path.GetFileName(new SqliteConnectionStringBuilder(connectionString).DataSource)
        };
        builder.LogTo(
            (identifier, _) => identifier == RelationalEventId.MigrationApplying,
            data =>
            {
                if (data is MigrationEventData migration)
                {
                    var identifier = migration.Migration.GetType().GetCustomAttribute<MigrationAttribute>()!.Id;
                    ApplyingMigration(logger, Environment.MachineName, Environment.ProcessId, store, identifier);
                }
            });
    }

    /// <summary>
    /// Identifies the node starting a migration; it is not a completion event.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="node">The machine name.</param>
    /// <param name="processId">The process identifier.</param>
    /// <param name="store">The physical database name.</param>
    /// <param name="migration">The EF migration identifier.</param>
    [LoggerMessage(LogLevel.Information, "Node {Node} process {ProcessId} applying SQL migration {Migration} to store {Store}")]
    internal static partial void ApplyingMigration(ILogger logger, string node, int processId, string store, string migration);
}
