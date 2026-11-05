// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Log messages for <see cref="EventSequenceMigrator"/>.
/// </summary>
internal static partial class EventSequenceMigratorLogging
{
    [LoggerMessage(LogLevel.Debug, "Creating event sequence table {TableName}")]
    internal static partial void CreatingEventSequenceTable(this ILogger<EventSequenceMigrator> logger, string tableName);

    [LoggerMessage(LogLevel.Information, "Adding Tags column to existing event sequence table {TableName}")]
    internal static partial void AddingTagsColumn(this ILogger<EventSequenceMigrator> logger, string tableName);

    [LoggerMessage(LogLevel.Information, "Adding EventSource column to existing event sequence table {TableName}")]
    internal static partial void AddingEventSourceColumn(this ILogger<EventSequenceMigrator> logger, string tableName);

    [LoggerMessage(LogLevel.Information, "Adding CompensationFor column to existing event sequence table {TableName}")]
    internal static partial void AddingCompensationForColumn(this ILogger<EventSequenceMigrator> logger, string tableName);

    [LoggerMessage(LogLevel.Warning, "Event sequence table {TableName} has {EventSourceIds} event source ids and {EventStreamIds} event streams stored in more than one spelling. Closed-stream entries written earlier apply to every spelling until reviewed")]
    internal static partial void SpellingVariantsFound(this ILogger<EventSequenceMigrator> logger, string tableName, long eventSourceIds, long eventStreamIds);

    [LoggerMessage(LogLevel.Debug, "Event sequence table {TableName} has no identifiers stored in more than one spelling")]
    internal static partial void NoSpellingVariants(this ILogger<EventSequenceMigrator> logger, string tableName);

    [LoggerMessage(LogLevel.Warning, "Event sequence table {TableName} has {EventSourceIds} event source ids and {EventStreamIds} event streams stored in more than one spelling that have not been marked as reviewed")]
    internal static partial void SpellingVariantsUnreviewed(this ILogger<EventSequenceMigrator> logger, string tableName, long eventSourceIds, long eventStreamIds);

    [LoggerMessage(LogLevel.Information, "Event sequence table {TableName} has not been checked for identifiers stored in more than one spelling. Run the migrate-only step to check it")]
    internal static partial void SpellingNotChecked(this ILogger<EventSequenceMigrator> logger, string tableName);
}
