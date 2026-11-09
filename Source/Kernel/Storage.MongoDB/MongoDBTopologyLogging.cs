// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB;

internal static partial class MongoDBTopologyLogging
{
    [LoggerMessage(LogLevel.Warning, "MongoDB is a standalone server without replication. Chronicle runs, but encryption key erasure cannot be confirmed and stays pending: confirmed erasure requires a replica set (a single-node replica set is enough)")]
    internal static partial void StandaloneServer(this ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Could not determine the MongoDB topology; assuming a replica set and probing again later ({ExceptionType})")]
    internal static partial void TopologyUnknown(this ILogger logger, string exceptionType);
}
