// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

#pragma warning disable SA1600 // Elements should be documented

internal static partial class TransientTimeoutsLogging
{
    [LoggerMessage(LogLevel.Warning, "The observer did not take the handled event counts for partition {Partition} in time and they are skipped: {Reason}")]
    internal static partial void HandledCountsReportTimedOut(this ILogger logger, Key partition, string reason);

    [LoggerMessage(LogLevel.Warning, "Handling events for partition {Partition} timed out and is tried once more: {Reason}")]
    internal static partial void RetryingAfterTimeout(this ILogger logger, Key partition, string reason);

    [LoggerMessage(LogLevel.Warning, "Job step timed out and is recorded as failed without reporting the partition as failed: {Reason}")]
    internal static partial void StepTimedOut(this ILogger logger, string reason);
}
