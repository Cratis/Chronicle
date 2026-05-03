// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Log messages for <see cref="ReadModelMigrator"/>.
/// </summary>
internal static partial class ReadModelMigratorLogging
{
    [LoggerMessage(LogLevel.Debug, "Creating read model table {TableName}")]
    internal static partial void CreatingReadModelTable(this ILogger<ReadModelMigrator> logger, string tableName);
}
