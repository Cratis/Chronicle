// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Opens the database-local provenance table independently of the connection's default schema.
/// </summary>
internal static class SinkProvenanceSchema
{
    static readonly ProjectedColumn[] _columns =
    [
        new("Id", typeof(string), true, false, false, false),
        new("Version", typeof(int), false, false, false, false),
        new("Restricted", typeof(bool), false, false, false, false)
    ];

    /// <summary>
    /// Gets the fixed metadata schema for a supported provider.
    /// </summary>
    /// <param name="databaseType">The physical provider.</param>
    /// <returns>The fixed metadata schema.</returns>
    internal static string Schema(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.PostgreSql => "public",
        DatabaseType.SqlServer => "dbo",
        DatabaseType.Sqlite => "main",
        _ => throw new InvalidOperationException("Read-model storage is not available.")
    };

    /// <summary>
    /// Selects metadata with an explicit physical schema, including SQLite where EF ignores table schemas.
    /// </summary>
    /// <param name="databaseType">The physical provider.</param>
    /// <returns>The fixed metadata query.</returns>
    internal static string Select(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.PostgreSql => "SELECT \"Id\", \"Version\", \"Restricted\" FROM public.\"__cratis_sink_provenance\"",
        DatabaseType.SqlServer => "SELECT [Id], [Version], [Restricted] FROM [dbo].[__cratis_sink_provenance]",
        DatabaseType.Sqlite => "SELECT [Id], [Version], [Restricted] FROM [main].[__cratis_sink_provenance]",
        _ => throw new InvalidOperationException("Read-model storage is not available.")
    };

    /// <summary>
    /// Opens the reserved table on the primary connection. Verify mode never creates it.
    /// </summary>
    /// <param name="database">The resolved sink database.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The marker context scope.</returns>
    internal static Task<DbContextScope<ReadModelDbContext>> Open(IDatabase database, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        database.ReadModelTable(eventStore, @namespace, SinkProvenanceNames.Metadata, _columns);

    /// <summary>
    /// Checks SQL Server destinations using the catalog's own identifier comparison rules.
    /// </summary>
    /// <param name="context">The metadata context.</param>
    /// <param name="containerName">The ordinary destination, if any.</param>
    /// <returns>The validation task.</returns>
    internal static async Task EnsureDestinationAvailable(ReadModelDbContext context, ReadModelContainerName? containerName)
    {
        if (containerName is null) return;
        SinkProvenanceNames.EnsureNotReserved(containerName);
        if (context.Database.GetDatabaseType() != DatabaseType.SqlServer) return;
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'dbo' AND t.name = N'__cratis_sink_provenance' AND t.name = @destination";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@destination";
            parameter.Value = containerName.Value;
            command.Parameters.Add(parameter);
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 0)
            {
                throw new InvalidOperationException("Read-model storage is not available.");
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    /// <summary>
    /// Creates only the fixed metadata table in Migrate mode. The caller validates its typed contents.
    /// </summary>
    /// <param name="context">The metadata context.</param>
    /// <returns>The provisioning task.</returns>
    internal static async Task Ensure(ReadModelDbContext context)
    {
        var databaseType = context.Database.GetDatabaseType();
        _ = Schema(databaseType);
        if (SinkCycleReceiptSchema.IsVerify(context)) return;
        var sql = CreateTable(databaseType);
        try
        {
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        catch (SqlException exception) when (exception.Number == 2714)
        {
            // A concurrent creator is accepted only after the caller reads and validates the table.
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.DuplicateTable or PostgresErrorCodes.UniqueViolation)
        {
            // PostgreSQL can race on the table or its catalog type; the typed read must still succeed.
        }
    }

    static string CreateTable(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.PostgreSql => "CREATE TABLE IF NOT EXISTS public.\"__cratis_sink_provenance\" (\"Id\" varchar(200) PRIMARY KEY, \"Version\" integer NOT NULL, \"Restricted\" boolean NOT NULL)",
        DatabaseType.SqlServer => "IF OBJECT_ID(N'dbo.__cratis_sink_provenance', N'U') IS NULL CREATE TABLE [dbo].[__cratis_sink_provenance] ([Id] nvarchar(200) NOT NULL PRIMARY KEY, [Version] int NOT NULL, [Restricted] bit NOT NULL)",
        DatabaseType.Sqlite => "CREATE TABLE IF NOT EXISTS [main].[__cratis_sink_provenance] ([Id] TEXT NOT NULL PRIMARY KEY, [Version] INTEGER NOT NULL, [Restricted] INTEGER NOT NULL)",
        _ => throw new InvalidOperationException("Read-model storage is not available.")
    };
}
