// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Occurrences;

internal static partial class OccurrenceRecoveryLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed expiring occurrence leases for worker {Worker} in {EventStore}/{Namespace}. Leases will expire at their deadlines")]
    internal static partial void FailedExpiringOccurrenceLeases(this ILogger logger, Exception exception, string worker, EventStoreName eventStore, string? @namespace);

    [LoggerMessage(LogLevel.Warning, "Failed discovering occurrence scopes for disconnected worker {Worker} in store {EventStore}. Leases will expire at their deadlines")]
    internal static partial void FailedDiscoveringOccurrenceScopes(this ILogger logger, Exception exception, string worker, string? eventStore);

    [LoggerMessage(LogLevel.Warning, "Failed waking occurrence queue {Trigger} in {EventStore}/{Namespace}. Its reminder can wake it later")]
    internal static partial void FailedWakingOccurrenceQueue(this ILogger logger, Exception exception, EventStoreName eventStore, string? @namespace, string trigger);
}
