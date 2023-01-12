// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Store.MongoDB.Observation;

/// <summary>
/// Holds the log messages for <see cref="MongoDBObserversState"/>.
/// </summary>
public static partial class MongoDBObserversStateLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Lost watch connection")]
    internal static partial void WatchConnectionLost(this ILogger<MongoDBObserversState> logger, Exception exception);
}
