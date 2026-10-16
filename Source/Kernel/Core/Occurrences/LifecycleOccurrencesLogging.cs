// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Occurrences;

internal static partial class LifecycleOccurrencesLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed reconciling lifecycle occurrence intent {OccurrenceId}. Its acknowledged prefix is retained for retry")]
    internal static partial void FailedReconcilingLifecycleIntent(this ILogger<LifecycleFanOut> logger, Exception exception, string occurrenceId);

    [LoggerMessage(LogLevel.Warning, "Failed reconciling lifecycle occurrences in {EventStore}. Startup will continue; durable intents remain pending for recovery")]
    internal static partial void FailedReconcilingLifecycleOccurrences(this ILogger logger, Exception exception, string eventStore);
}
