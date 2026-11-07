// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

internal static partial class RepeatMoveSystemEventStoreDatabasesLogging
{
    [LoggerMessage(LogLevel.Information, "Moving the System event store's data to its configured location, again, for installations where the first attempt did nothing")]
    internal static partial void MovingToConfiguredLocation(this ILogger<RepeatMoveSystemEventStoreDatabases> logger);
}
