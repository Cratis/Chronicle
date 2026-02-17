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
}
