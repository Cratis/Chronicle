// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Log messages for <see cref="ITableMigrator{TContext}"/>.
/// </summary>
internal static partial class TableMigratorLogging
{
    [LoggerMessage(LogLevel.Information, "Ensuring table {TableName} is migrated")]
    internal static partial void EnsuringTableMigrated(this ILogger logger, string tableName);

    [LoggerMessage(LogLevel.Information, "Created table {TableName}")]
    internal static partial void CreatedTable(this ILogger logger, string tableName);

    [LoggerMessage(LogLevel.Debug, "Table {TableName} already exists")]
    internal static partial void TableAlreadyExists(this ILogger logger, string tableName);

    [LoggerMessage(LogLevel.Error, "Failed to migrate table {TableName}")]
    internal static partial void FailedToMigrateTable(this ILogger logger, Exception ex, string tableName);
}
