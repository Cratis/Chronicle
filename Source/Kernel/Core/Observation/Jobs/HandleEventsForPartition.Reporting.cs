// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

public partial class HandleEventsForPartition
{
    /// <summary>
    /// Creates the result for a failure the observer could not be told about. It is handed to the job as transient, so
    /// the job runs the step again and reports the partition as failed when the attempts run out - a failure is never
    /// left without a failed partition to retry it.
    /// </summary>
    /// <param name="partition">The partition the step was handling.</param>
    /// <param name="failedAt">The sequence number the partition failed at.</param>
    /// <param name="lastSuccessfullyHandled">The last successfully handled event sequence number.</param>
    /// <param name="error">The failure.</param>
    /// <returns>The <see cref="JobStepResult"/>.</returns>
    static JobStepResult UnreportedFailure(Key partition, EventSequenceNumber failedAt, EventSequenceNumber lastSuccessfullyHandled, Exception error)
    {
        var messages = TransientTimeouts.ShortenForStep(error.GetAllMessages());
        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            CreateResult(lastSuccessfullyHandled) with { TransientFailure = new(partition, failedAt, messages) },
            messages,
            string.Empty));
    }

    Task ReportPartitionDeferred(HandleEventsForPartitionState currentState, PartitionDeferral deferral) =>
        currentState.ReplayEpoch is { } replayEpoch
            ? _observer.PartitionDeferred(currentState.Partition, deferral, currentState.EpisodeId, currentState.DeferralId, replayEpoch)
            : _observer.PartitionDeferred(currentState.Partition, deferral, currentState.EpisodeId, currentState.DeferralId);

    Task ReportPartitionFailed(HandleEventsForPartitionState currentState, EventSequenceNumber failedAt, BatchFailure failure) =>
        ReportPartitionFailed(currentState, failedAt, failure.Messages, failure);

    Task ReportPartitionFailed(HandleEventsForPartitionState currentState, EventSequenceNumber failedAt, IEnumerable<string> messages, BatchFailure failure) =>
        currentState.ReplayEpoch is { } replayEpoch
            ? _observer.PartitionFailed(_eventSourceId, failedAt, messages, failure.StackTrace, failure.Kind, currentState.EpisodeId, currentState.DeferralId, new PartitionFailureContext(failure.Policy, null, replayEpoch))
            : _observer.PartitionFailed(_eventSourceId, failedAt, messages, failure.StackTrace, failure.Kind, currentState.EpisodeId, currentState.DeferralId, failure.Policy);

    async Task<bool> TryReportPartitionFailed(HandleEventsForPartitionState currentState, EventSequenceNumber failedAt, Exception error)
    {
        try
        {
            await ReportPartitionFailed(currentState, failedAt, error.GetAllMessages(), new([], error.StackTrace ?? string.Empty, error.ToFailureKind()));
            return true;
        }
        catch (Exception reportError)
        {
            logger.FailedReportingPartitionFailed(reportError, _eventSourceId, failedAt);
            return false;
        }
    }

    /// <summary>
    /// Reports progress from the step's background work. The state update and the debounced write run as one
    /// callback on the activation, so a stopped or deactivating step either records the whole batch or none of it.
    /// </summary>
    /// <param name="lastHandled">The last handled event sequence number.</param>
    /// <returns>True when the progress was recorded; false when the step refused the callback because its run is closed.</returns>
    async Task<bool> ReportFromWork(EventSequenceNumber lastHandled)
    {
        await ConfirmBulkWindowDurability();
        var reported = await TryInvokeOnActivation(() => ReportNewSuccessfullyHandledEvent(lastHandled));
        if (reported.TryGetError(out var rejection))
        {
            logger.CheckpointCallbackRejected(_eventSourceId, rejection, lastHandled);
            return false;
        }

        return true;
    }

    Task ReportHandledEvents(Key partition, AppendedEvent[] handledEvents, EventSequenceNumber handledUpTo, CancellationToken cancellationToken)
    {
        var counts = handledEvents
            .Where(e => e.Context.SequenceNumber <= handledUpTo)
            .CountByEventType();
        return TransientTimeouts.ReportHandledEventsWithoutFailing(
            _observer,
            new(partition, counts, JobStepId, handledUpTo),
            TransientRetryDelay,
            logger,
            cancellationToken);
    }
}
