// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

internal static partial class DatabaseNameResolverLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Database identity {Identity} does not own shared database {Candidate}; no data belonging to it was found. Binding it to {PhysicalName}.")]
    internal static partial void SharedDatabase(this ILogger<DatabaseNameResolver> logger, string identity, string candidate, string physicalName);
}
