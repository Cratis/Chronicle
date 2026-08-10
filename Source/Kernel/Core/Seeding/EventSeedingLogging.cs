// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Seeding;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class EventSeedingLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Seeding events for event store '{EventStore}' in namespace '{Namespace}'")]
    internal static partial void SeedingEvents(this ILogger<EventSeeding> logger, string eventStore, string @namespace);

    [LoggerMessage(LogLevel.Debug, "Appending {Count} new seeded events")]
    internal static partial void AppendingSeededEvents(this ILogger<EventSeeding> logger, int count);

    [LoggerMessage(LogLevel.Debug, "All events have already been seeded, skipping")]
    internal static partial void AllEventsAlreadySeeded(this ILogger<EventSeeding> logger);

    [LoggerMessage(LogLevel.Debug, "Applying global seeds to namespace '{Namespace}'")]
    internal static partial void ApplyingSeedsToNamespace(this ILogger<EventSeeding> logger, string @namespace);

    [LoggerMessage(LogLevel.Error, "The event sequence rejected a batch of {Count} seeded events for event store '{EventStore}' in namespace '{Namespace}' - none of them were appended. Violated constraints: {ConstraintNames}. Errors: {ErrorCount}. Concurrency violations: {ConcurrencyViolationCount}. The batch has not been recorded as seeded and will be offered again on the next seeding run.")]
    internal static partial void SeededEventsRejected(this ILogger<EventSeeding> logger, int count, string eventStore, string @namespace, string constraintNames, int errorCount, int concurrencyViolationCount);

    [LoggerMessage(LogLevel.Error, "One or more namespaces did not seed everything they were given for event store '{EventStore}' - the global seed tracking is left uncommitted so the entries are dispatched again on the next seeding run")]
    internal static partial void NamespaceSeedingIncomplete(this ILogger<EventSeeding> logger, string eventStore);
}
