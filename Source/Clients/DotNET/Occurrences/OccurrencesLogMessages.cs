// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Occurrences;

internal static partial class OccurrencesLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Lifecycle shutdown recording or delivery did not complete within the host lifetime; essential cleanup requires previously recorded durable intent")]
    internal static partial void LifecycleShutdownIncomplete(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Occurrence worker for {Trigger} failed; delivery will resume from durable state")]
    internal static partial void OccurrenceWorkerFailed(this ILogger logger, Exception exception, string trigger);

    [LoggerMessage(LogLevel.Warning, "Renewal for occurrence {Occurrence} failed; cancelling the attempt")]
    internal static partial void OccurrenceLeaseRenewalFailed(this ILogger logger, Exception exception, string occurrence);
}
