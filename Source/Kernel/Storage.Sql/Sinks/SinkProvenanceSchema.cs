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
/// Addresses metadata in the configured storage schema, never a route's payload search path.
/// </summary>
internal static class SinkProvenanceSchema
{
    /// <summary>
    /// The retained legacy marker representation.
    /// </summary>
    internal static readonly ProjectedColumn[] MarkerColumns =
    [
        new("Id", typeof(string), true, false, false, false),
        new("Version", typeof(int), false, false, false, false),
        new("Restricted", typeof(bool), false, false, false, false)
    ];

    /// <summary>
    /// The durable physical identity and inventory representation.
    /// </summary>
    internal static readonly ProjectedColumn[] InventoryColumns =
    [
        .. MarkerColumns,
        new("DatabaseId", typeof(string), false, false, false, false),
        new("InventoryVersion", typeof(int), false, false, false, false)
    ];

    /// <summary>
    /// Resolves the canonical metadata schema independently of the payload search path.
    /// </summary>
    /// <param name="databaseType">The provider.</param>
    /// <param name="configured">The configured storage schema.</param>
    /// <returns>The canonical metadata schema.</returns>
    /// <exception cref="InvalidOperationException">The provider is unsupported.</exception>
    internal static string Schema(DatabaseType databaseType, string? configured = null) => databaseType switch
    {
        DatabaseType.PostgreSql => string.IsNullOrEmpty(configured) ? "public" : configured,
        DatabaseType.SqlServer => string.IsNullOrEmpty(configured) ? "dbo" : configured,
        DatabaseType.Sqlite => "main",
        _ => throw new InvalidOperationException("Read-model storage is not available.")
    };

    /// <summary>
    /// Delimits and escapes an identifier for the selected provider.
    /// </summary>
    /// <param name="type">The provider.</param>
    /// <param name="name">The identifier.</param>
    /// <returns>The delimited identifier.</returns>
    internal static string Quote(DatabaseType type, string name) => type == DatabaseType.PostgreSql
        ? $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
        : $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";

    /// <summary>
    /// Qualifies a metadata table with the configured, delimited storage schema.
    /// </summary>
    /// <param name="context">The actual context.</param>
    /// <param name="table">The metadata table.</param>
    /// <returns>The qualified identifier.</returns>
    internal static string Qualified(ReadModelDbContext context, string table) =>
        $"{Quote(context.Database.GetDatabaseType(), Schema(context.Database.GetDatabaseType(), context.ProvenanceSchema))}.{Quote(context.Database.GetDatabaseType(), table)}";

    /// <summary>
    /// Selects the legacy marker in the provider's default metadata schema.
    /// </summary>
    /// <param name="type">The provider.</param>
    /// <returns>The metadata query.</returns>
    internal static string Select(DatabaseType type) =>
        $"SELECT {string.Join(", ", MarkerColumns.Select(_ => Quote(type, _.Name)))} FROM {Quote(type, Schema(type))}.{Quote(type, SinkProvenanceNames.Metadata)}";

    /// <summary>
    /// Selects the context's metadata shape from the configured storage schema.
    /// </summary>
    /// <param name="context">The actual metadata context.</param>
    /// <returns>The metadata query.</returns>
    internal static string Select(ReadModelDbContext context) =>
        $"SELECT {string.Join(", ", context.Columns.Select(_ => Quote(context.Database.GetDatabaseType(), _.Name)))} FROM {Qualified(context, context.TableName)}";

    /// <summary>
    /// Opens legacy marker access on an optionally admitted physical target.
    /// </summary>
    /// <param name="database">The database acquisition service.</param>
    /// <param name="eventStore">The store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="admittedTarget">The expected physical target.</param>
    /// <returns>The metadata scope.</returns>
    internal static Task<DbContextScope<ReadModelDbContext>> Open(IDatabase database, EventStoreName eventStore, EventStoreNamespaceName @namespace, SinkProvenanceTarget? admittedTarget = null) =>
        database.ReadModelTable(eventStore, @namespace, SinkProvenanceNames.Metadata, MarkerColumns, admittedTarget: admittedTarget);

    /// <summary>
    /// Refuses metadata collisions using the actual database collation.
    /// </summary>
    /// <param name="context">The metadata context.</param>
    /// <param name="containerName">The ordinary sink destination.</param>
    /// <returns>The validation task.</returns>
    /// <exception cref="InvalidOperationException">The destination collides with metadata.</exception>
    internal static async Task EnsureDestinationAvailable(ReadModelDbContext context, ReadModelContainerName? containerName)
    {
        if (containerName is null) return;
        SinkProvenanceNames.EnsureNotReserved(containerName);
        if (context.Database.GetDatabaseType() != DatabaseType.SqlServer) return;
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = @schema AND t.name IN (@marker, @inventory) AND t.name = @destination";
            Add(command, "@schema", Schema(DatabaseType.SqlServer, context.ProvenanceSchema));
            Add(command, "@marker", SinkProvenanceNames.Metadata);
            Add(command, "@inventory", SinkProvenanceNames.Inventory);
            Add(command, "@destination", containerName.Value);
            if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 0) throw new InvalidOperationException("Read-model storage is not available.");
        }
        finally { await context.Database.CloseConnectionAsync(); }
    }

    /// <summary>
    /// Creates metadata schema only when schema application is permitted.
    /// </summary>
    /// <param name="context">The metadata context.</param>
    /// <returns>The schema preparation task.</returns>
    internal static async Task Ensure(ReadModelDbContext context)
    {
        var type = context.Database.GetDatabaseType();
        _ = Schema(type);
        if (SinkCycleReceiptSchema.IsVerify(context)) return;
        try { await context.Database.ExecuteSqlRawAsync(CreateTable(context)); }
        catch (SqlException exception) when (exception.Number == 2714) { /* Validate the concurrent winner on read. */ }
        catch (PostgresException exception) when (string.Equals(exception.SqlState, PostgresErrorCodes.DuplicateTable, StringComparison.Ordinal) || string.Equals(exception.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal)) { /* Validate the concurrent winner on read. */ }
    }

    /// <summary>
    /// Adds a value parameter without interpolating it into SQL.
    /// </summary>
    /// <param name="command">The metadata command.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    internal static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Determines whether the table is Chronicle-owned database metadata.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns>Whether the name denotes provenance metadata.</returns>
    internal static bool IsMetadata(string tableName) =>
        string.Equals(tableName, SinkProvenanceNames.Metadata, StringComparison.Ordinal) ||
        string.Equals(tableName, SinkProvenanceNames.Inventory, StringComparison.Ordinal);

    static string CreateTable(ReadModelDbContext context)
    {
        var type = context.Database.GetDatabaseType();
        var schema = Schema(type, context.ProvenanceSchema);
        var table = Qualified(context, context.TableName);
        var id = Quote(type, "Id");
        var version = Quote(type, "Version");
        var restricted = Quote(type, "Restricted");
        var inventory = context.TableName == SinkProvenanceNames.Inventory;
        var extra = inventory ? $", {Quote(type, "DatabaseId")} {(type == DatabaseType.SqlServer ? "nvarchar(200)" : "varchar(200)")} NOT NULL, {Quote(type, "InventoryVersion")} int NOT NULL" : string.Empty;
        return type switch
        {
            DatabaseType.PostgreSql => $"CREATE SCHEMA IF NOT EXISTS {Quote(type, schema)}; CREATE TABLE IF NOT EXISTS {table} ({id} varchar(200) PRIMARY KEY, {version} integer NOT NULL, {restricted} boolean NOT NULL{extra})",
            DatabaseType.SqlServer => $"IF SCHEMA_ID(N'{Literal(schema)}') IS NULL EXEC(N'CREATE SCHEMA {Literal(Quote(type, schema))}'); IF OBJECT_ID(N'{Literal(table)}', N'U') IS NULL CREATE TABLE {table} ({id} nvarchar(200) NOT NULL PRIMARY KEY, {version} int NOT NULL, {restricted} bit NOT NULL{extra})",
            DatabaseType.Sqlite => $"CREATE TABLE IF NOT EXISTS {table} ({id} TEXT NOT NULL PRIMARY KEY, {version} INTEGER NOT NULL, {restricted} INTEGER NOT NULL{extra})",
            _ => throw new InvalidOperationException("Read-model storage is not available.")
        };
    }

    static string Literal(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
