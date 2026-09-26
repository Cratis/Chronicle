// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql;

internal static partial class DatabaseLogging
{
    [LoggerMessage(LogLevel.Warning, "The derived database name '{LogicalName}' exceeds what the database server allows for an identifier and is used as '{PhysicalName}'. Look for the physical name when finding this database by hand.")]
    internal static partial void DerivedDatabaseNameTruncated(this ILogger<Database> logger, string logicalName, string physicalName);
}
