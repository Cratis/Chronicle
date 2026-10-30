// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Log messages for <see cref="ReadModelMigrator"/>.
/// </summary>
internal static partial class ReadModelMigratorLogging
{
    [LoggerMessage(LogLevel.Warning, "Shadow-index DDL failed for index {IndexName} on table {TableName}")]
    internal static partial void ShadowIndexDdlFailed(this ILogger<ReadModelMigrator> logger, string tableName, string indexName, Exception exception);

    [LoggerMessage(LogLevel.Information, "Another kernel is building the initialized-marker index on read model table {TableName}; it will be ensured again on a later activation")]
    internal static partial void InitializedMarkerIndexDeferred(this ILogger<ReadModelMigrator> logger, string tableName);

    [LoggerMessage(LogLevel.Debug, "Creating read model table {TableName}")]
    internal static partial void CreatingReadModelTable(this ILogger<ReadModelMigrator> logger, string tableName);

    [LoggerMessage(LogLevel.Debug, "Adding {ColumnCount} missing column(s) to read model table {TableName}: {Columns}")]
    internal static partial void AddingMissingColumns(this ILogger<ReadModelMigrator> logger, int columnCount, string tableName, string columns);
}
