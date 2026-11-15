// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

internal static partial class MoveSystemEventStoreDatabasesLogging
{
    [LoggerMessage(LogLevel.Information, "Moving the System event store's data to its configured location")]
    internal static partial void MovingToConfiguredLocation(this ILogger<MoveSystemEventStoreDatabases> logger);

    [LoggerMessage(LogLevel.Warning, "Could not move the System event store's data to its configured location, so it was left where it is and the kernel continues to start. If the data is in databases under the old name, check that the MongoDB user may read them, or move them by hand")]
    internal static partial void FailedMovingToConfiguredLocation(this ILogger<MoveSystemEventStoreDatabases> logger, Exception exception);
}
