// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a job for retrying a failed partition.
/// </summary>
/// <param name="replayStateServiceClient"><see cref="IObserverServiceClient"/>.</param>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
public class ReplayObserverPartition(
    IObserverServiceClient replayStateServiceClient,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ReplayObserverPartition> logger) : Job<ReplayObserverPartitionRequest, JobStateWithLastHandledEvent>, IReplayObserverPartition
{
    /// <inheritdoc/>
    protected override async Task OnBeforeStartingJobSteps()
    {
        await replayStateServiceClient.BeginReplayPartitionFor(State.ObserverDetails, Request.Key);
    }

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
        await observer.PartitionReplayed(Request.Key, State.LastHandledEventSequenceNumber, lastScanned, replayedEventTypes);
    }

    /// <inheritdoc/>
    protected override Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result)
    {
        State.HandleResult(result, jsonSerializerOptions);
        return Task.CompletedTask;
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
                    request.EventTypes))
        }.ToImmutableList();

        return Task.FromResult<IImmutableList<JobStepDetails>>(steps);
    }
}
