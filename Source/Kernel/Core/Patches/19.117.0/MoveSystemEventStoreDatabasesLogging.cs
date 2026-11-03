// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

internal static partial class MoveSystemEventStoreDatabasesLogging
{
    [LoggerMessage(LogLevel.Information, "Moving the System event store's data to its configured location")]
    internal static partial void MovingToConfiguredLocation(this ILogger<MoveSystemEventStoreDatabases> logger);

    [LoggerMessage(LogLevel.Information, "Moving the System event store's data back to its legacy location")]
    internal static partial void MovingToLegacyLocation(this ILogger<MoveSystemEventStoreDatabases> logger);
}
