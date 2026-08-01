// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Jobs;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Observation.Jobs;

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1402 // File may only contain a single type

internal static partial class ObservationJobLogMessages
{
    [LoggerMessage(LogLevel.Warning, "Not all events were handled after performing '{JobName}' job. The last handled event sequence number was {LastSequenceNumber}")]
    internal static partial void NotAllEventsWereHandled(this ILogger<IJob> logger, string jobName, EventSequenceNumber lastSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "No events were handled after performing '{JobName}' job")]
    internal static partial void NoEventsWereHandled(this ILogger<IJob> logger, string jobName);

    [LoggerMessage(LogLevel.Information, "Replay progress: {CompletedSteps}/{TotalSteps} partitions completed ({PercentComplete:F1}%). Last handled sequence number: {LastSequenceNumber}")]
    internal static partial void ReplayProgress(this ILogger<ReplayObserver> logger, int completedSteps, int totalSteps, double percentComplete, EventSequenceNumber lastSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "Recovery of failed partition '{Partition}' handled no events but there are still events from sequence number {FromSequenceNumber} to handle. Keeping the partition failed rather than clearing it without running the handler")]
    internal static partial void NotClearingFailedPartitionWithEventsLeftToHandle(this ILogger<IJob> logger, Key partition, EventSequenceNumber fromSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "Clearing failed partition '{Partition}' without running the handler - there is no event from sequence number {FromSequenceNumber} left to handle, so the failure record is stale")]
    internal static partial void ClearingFailedPartitionWithNothingLeftToHandle(this ILogger<IJob> logger, Key partition, EventSequenceNumber fromSequenceNumber);

    [LoggerMessage(LogLevel.Warning, "Could not check whether failed partition '{Partition}' has events from sequence number {FromSequenceNumber} left to handle. Keeping the partition failed")]
    internal static partial void FailedCheckingForEventsLeftToHandle(this ILogger<IJob> logger, Exception exception, Key partition, EventSequenceNumber fromSequenceNumber);
}
