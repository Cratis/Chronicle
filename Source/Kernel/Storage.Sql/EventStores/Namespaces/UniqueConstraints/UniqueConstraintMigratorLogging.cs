// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Log messages for <see cref="UniqueConstraintMigrator"/>.
/// </summary>
internal static partial class UniqueConstraintMigratorLogging
{
    [LoggerMessage(LogLevel.Debug, "Creating unique constraint table {TableName}")]
    internal static partial void CreatingUniqueConstraintTable(this ILogger<UniqueConstraintMigrator> logger, string tableName);

    [LoggerMessage(LogLevel.Warning, "Unique constraint table {TableName} has {DuplicateCount} duplicate values; keeping a non-unique index where supported. Reconcile the claims or reindex; unique index creation will be retried at the next process start")]
    internal static partial void FallingBackToNonUniqueIndex(this ILogger<UniqueConstraintMigrator> logger, string tableName, long duplicateCount);

    [LoggerMessage(LogLevel.Warning, "Skipping Value column narrowing and unique indexing for table {TableName}: {OversizedCount} rows exceed 200 characters and {LegacyCount} rows are not hashes. No values were truncated; see https://github.com/Cratis/Chronicle/issues/4866")]
    internal static partial void SkippingValueColumnNarrowing(this ILogger<UniqueConstraintMigrator> logger, string tableName, long oversizedCount, long legacyCount);
}
