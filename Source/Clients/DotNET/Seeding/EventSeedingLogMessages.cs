// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding;

internal static partial class EventSeedingLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Failed to activate event seeder of type '{SeederType}' - skipping seeder")]
    internal static partial void FailedToActivateSeeder(this ILogger logger, Type seederType, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Failed to run event seeder of type '{SeederType}' - global declaration will not be sent")]
    internal static partial void FailedToRunSeeder(this ILogger logger, Type seederType, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Skipping global seed declaration because discovery failed for seeder types '{SeederTypes}'; keeping the previous declaration and retrying discovery on the next registration")]
    internal static partial void GlobalSeedDeclarationSkipped(this ILogger logger, string seederTypes);
}
