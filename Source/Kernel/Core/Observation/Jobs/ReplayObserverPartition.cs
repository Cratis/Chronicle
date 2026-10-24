// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a job for retrying a failed partition.
/// </summary>
/// <param name="replayStateServiceClient"><see cref="IObserverServiceClient"/>.</param>
/// <param name="storage"><see cref="IStorage"/> for reading the persisted projection definition on resume.</param>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
public class ReplayObserverPartition(
    IObserverServiceClient replayStateServiceClient,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ReplayObserverPartition> logger) : Job<ReplayObserverPartitionRequest, JobStateWithLastHandledEvent>, IReplayObserverPartition
{
    /// <inheritdoc/>
    protected override async Task OnBeforeStartingJobSteps()
    {
        await replayStateServiceClient.BeginReplayPartitionFor(State.ObserverDetails, Request.Key);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Only the admission check is repeated: a persisted partition replay of a projection whose stored definition is
    /// globally scoped must not continue writing into the shared sink. The replay itself is not begun again, so
    /// reactors and reducers are not told twice that their partition replay started.
    /// </remarks>
    protected override Task OnBeforeResumingJobSteps() =>
        GlobalProjectionReplay.EnsureSupported(storage, Request.ObserverKey, Request.ObserverType);

    /// <inheritdoc/>
    protected override async Task OnAllStepsCompleted()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        if (State is { HandledAllEvents: false, LastHandledEventSequenceNumber.IsActualValue: true })
        {
            logger.NotAllEventsWereHandled(nameof(ReplayObserverPartition), State.LastHandledEventSequenceNumber);
        }

        await replayStateServiceClient.EndReplayPartitionFor(State.ObserverDetails, Request.Key);

        if (!State.LastHandledEventSequenceNumber.IsActualValue)
        {
            logger.NoEventsWereHandled(nameof(ReplayObserverPartition));
        }

        // The observer is always told the replay is over, even when it handled nothing - a replay that read only
        // events the observer's filters exclude still has to stop holding back live delivery for the partition.
        // How far it read is only trusted from a replay that completed, so a stopped replay never skips events.
        var lastScanned = State.HandledAllEvents ? State.LastScannedEventSequenceNumber : EventSequenceNumber.Unavailable;

        // A selected-type replay cannot prove that it handled the event which failed for this partition.
        var replayedEventTypes = State.HandledAllEvents && Request.ReplaysAllEventTypes ? Request.EventTypes.ToArray() : [];
        await observer.PartitionReplayed(Request.Key, State.LastHandledEventSequenceNumber, lastScanned, replayedEventTypes, JobId);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A step that timed out is run again - the callee was too busy, which says nothing about the events - and the
    /// partition is reported as failed only when the attempts are used up.
    /// </remarks>
    protected override bool ShouldRetryStep(JobStepId stepId, JobStepResult result, int attempt) =>
        TransientTimeouts.ShouldRetry(result, attempt, jsonSerializerOptions);

    /// <inheritdoc/>
    protected override async Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result)
    {
        await TransientTimeouts.ReportPartitionFailedWhenAttemptsAreExhausted(GrainFactory.GetGrain<IObserver>(Request.ObserverKey), result, jsonSerializerOptions);
        State.HandleResult(result, jsonSerializerOptions);
    }

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(ReplayObserverPartitionRequest request)
    {
        var steps = new[]
        {
            CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverKey,
                    request.ObserverType,
                    request.Key,
                    request.FromSequenceNumber,
                    request.ToSequenceNumber,
                    EventObservationState.Replay,
                    request.EventTypes)
                {
                    ReaderScope = request.ReaderScope
                })
        }.ToImmutableList();

        return Task.FromResult<IImmutableList<JobStepDetails>>(steps);
    }
}
