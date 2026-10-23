// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

#pragma warning disable SA1600 // Elements should be documented

internal static partial class TransientTimeoutsLogging
{
    [LoggerMessage(LogLevel.Warning, "The observer did not take the handled event counts for partition {Partition} (attempt {Attempt}) and they are sent again: {Reason}")]
    internal static partial void RetryingHandledCountsReport(this ILogger logger, Key partition, int attempt, string reason);

    [LoggerMessage(LogLevel.Error, "The handled event counts for partition {Partition} up to event sequence number {HandledUpTo} could not be reported to the observer and are missing from its statistics. The events themselves were handled: {Reason}")]
    internal static partial void HandledCountsReportAbandoned(this ILogger logger, Key partition, EventSequenceNumber handledUpTo, string reason);

    [LoggerMessage(LogLevel.Warning, "Handling events for partition {Partition} timed out and is tried once more: {Reason}")]
    internal static partial void RetryingAfterTimeout(this ILogger logger, Key partition, string reason);

    [LoggerMessage(LogLevel.Warning, "Job step timed out and is recorded as failed without reporting the partition as failed: {Reason}")]
    internal static partial void StepTimedOut(this ILogger logger, string reason);
}
