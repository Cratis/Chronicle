// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding;

internal static partial class EventSeedingLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Failed to activate event seeder of type '{SeederType}' - skipping seeder")]
    internal static partial void FailedToActivateSeeder(this ILogger logger, Type seederType, Exception exception);
}
