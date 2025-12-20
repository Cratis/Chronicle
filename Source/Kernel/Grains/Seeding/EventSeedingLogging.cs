// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Seeding;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class EventSeedingLogMessages
{
    [LoggerMessage(LogLevel.Information, "Seeding events for event store '{EventStore}' in namespace '{Namespace}'")]
    internal static partial void SeedingEvents(this ILogger<EventSeeding> logger, string eventStore, string @namespace);

    [LoggerMessage(LogLevel.Information, "Appending {Count} new seeded events")]
    internal static partial void AppendingSeededEvents(this ILogger<EventSeeding> logger, int count);

    [LoggerMessage(LogLevel.Information, "All events have already been seeded, skipping")]
    internal static partial void AllEventsAlreadySeeded(this ILogger<EventSeeding> logger);
}
