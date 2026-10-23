// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// The registry of nullable, default-free kernel shadow columns, deliberately outside the EF model.
/// </summary>
internal static partial class ShadowColumns
{
    static readonly Definition[] _definitions =
    [
        new("__order_{column}", provider => provider == DatabaseType.Sqlite, "TEXT", context => context.Columns.Where(SqliteDecimalSortKey.IsDecimal), SqliteDecimalSortKey.SqlExpressionFor, column => column.IsIndexed)
    ];

    /// <summary>
    /// Resolves the applicable shadows, preserving schema properties when names collide.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <param name="collision">Called for every shadow skipped because the schema owns its name.</param>
    /// <returns>The non-colliding columns owned by this kernel.</returns>
    internal static IReadOnlyList<AddColumnOperation> For(ReadModelDbContext context, Action<string>? collision = null)
    {
        var helper = context.GetService<ISqlGenerationHelper>();
        return DefinitionsFor(context).SelectMany(definition => definition.Sources(context).Select(column =>
        {
            var name = NameFor(definition, column);
            if (context.Columns.Any(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                collision?.Invoke(name);
                return null;
            }
            return new AddColumnOperation
            {
                Name = name, Table = context.TableName, ClrType = typeof(string), ColumnType = definition.ColumnType,
                IsNullable = true, DefaultValue = null,
                ComputedColumnSql = definition.GeneratedSql?.Invoke(helper.DelimitIdentifier(column.Name)),
                IsStored = definition.GeneratedSql is null ? null : false
            };
        })).OfType<AddColumnOperation>().ToArray();
    }

    /// <summary>
    /// Gets the shadows whose registry entry requires an index for this schema.
    /// </summary>
    /// <param name="context">The read-model context.</param>
    /// <returns>The expected indexed shadow names.</returns>
    internal static string[] IndexedFor(ReadModelDbContext context)
    {
        var available = For(context).Select(column => column.Name).ToHashSet(StringComparer.Ordinal);
        return DefinitionsFor(context).SelectMany(definition => definition.Sources(context)
            .Where(definition.Indexed).Select(column => NameFor(definition, column))).Where(available.Contains).ToArray();
    }

    /// <summary>
    /// Runs a shadow-specific operation, repairing a stale migration cache and retrying once if needed.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="context">The read-model context.</param>
    /// <param name="column">The required shadow name.</param>
    /// <param name="operation">The operation, outside any write transaction.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="InvalidOperationException">The schema owns the shadow name or the provider is unsupported.</exception>
    internal static async Task<T> Access<T>(ReadModelDbContext context, string column, Func<Task<T>> operation)
    {
        if (!For(context).Any(shadow => shadow.Name == column))
        {
            throw new InvalidOperationException($"Shadow column '{column}' is unavailable for table '{context.TableName}'; its name is owned by the read-model schema or its provider is unsupported. Numeric decimal ordering and exact narrowing cannot proceed.");
        }
        try
        {
            return await operation();
        }
        catch (Exception exception) when (IsMissingColumn(exception))
        {
            await context.RepairShadowColumns();
            return await operation();
        }
    }

    /// <summary>
    /// Recognizes provider missing-column errors, including errors wrapped by EF.
    /// </summary>
    /// <param name="exception">The provider or wrapper exception.</param>
    /// <returns>Whether a retry after shadow migration is warranted.</returns>
    internal static bool IsMissingColumn(Exception exception) => exception switch
    {
        SqliteException sqlite => sqlite.SqliteErrorCode == 1 && sqlite.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase),
        PostgresException postgres => postgres.SqlState == "42703",
        SqlException sqlServer => sqlServer.Number == 207,
        _ => exception.InnerException is not null && IsMissingColumn(exception.InnerException)
    };

    /// <summary>
    /// Warns when preserving a schema property instead of installing an internal shadow.
    /// </summary>
    /// <param name="logger">The migrator logger.</param>
    /// <param name="table">The affected table.</param>
    /// <param name="column">The colliding shadow name.</param>
    [LoggerMessage(LogLevel.Warning, "Skipping shadow column {Column} on {Table}: the read-model schema owns this name; shadow-dependent features will refuse this table")]
    internal static partial void Collision(ILogger logger, string table, string column);

    static IEnumerable<Definition> DefinitionsFor(ReadModelDbContext context) =>
        _definitions.Where(definition => definition.Provider(context.Database.GetDatabaseType()));

    static string NameFor(Definition definition, ProjectedColumn column) => definition.NamePattern.Replace("{column}", column.Name, StringComparison.Ordinal);

    sealed record Definition(
        string NamePattern,
        Func<DatabaseType, bool> Provider,
        string ColumnType,
        Func<ReadModelDbContext, IEnumerable<ProjectedColumn>> Sources,
        Func<string, string>? GeneratedSql,
        Func<ProjectedColumn, bool> Indexed);
}
