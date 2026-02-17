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
}
