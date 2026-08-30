// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a job for catching up an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReplayObserver"/> class.
/// </remarks>
/// <param name="catchupServiceClient"><see cref="IObserverServiceClient"/>.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
public class CatchUpObserver(
    IObserverServiceClient catchupServiceClient,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<CatchUpObserver> logger) : Job<CatchUpObserverRequest, JobStateWithLastHandledEvent>, ICatchUpObserver
{
    /// <inheritdoc/>
    /// <remarks>
    /// Catching up is reached through Subscribe, and behind Subscribe sits the client's registration call with a
    /// response timeout on it. Enumerating the observer's event sources and bringing up their steps must therefore
    /// not be billed to that call.
    /// </remarks>
    protected override bool StartStepsInBackground => true;

    /// <inheritdoc/>
    protected override async Task OnBeforeStartingJobSteps()
    {
        await catchupServiceClient.BeginCatchupFor(State.ObserverDetails);
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeResumingJobSteps()
    {
        await catchupServiceClient.ResumeCatchupFor(State.ObserverDetails);
    }

    /// <inheritdoc/>
    protected override async Task OnAllStepsCompleted()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        await catchupServiceClient.EndCatchupFor(State.ObserverDetails);

        if (!AllStepsCompletedSuccessfully)
        {
            if (State.LastHandledEventSequenceNumber.IsActualValue)
            {
                logger.NotAllEventsWereHandled(nameof(CatchUpObserver), State.LastHandledEventSequenceNumber);
            }
            else
            {
                logger.NoEventsWereHandled(nameof(CatchUpObserver));
            }
        }

        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);

        // Fire-and-forget to avoid a reentrancy deadlock when OnAllStepsCompleted is called from
        // inside job.Start() (e.g. the 0-step case). The Observer grain may still be executing
        // CatchUp(), so CaughtUp() would be queued and deadlock. Returning first lets the Observer
        // grain become free to process CaughtUp().
        _ = observer.CaughtUp(State.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    protected override Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result)
    {
        State.HandleResult(result, jsonSerializerOptions);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.ObserverKey.ObserverId}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume()
    {
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        return observer.IsSubscribed();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A projection catches up through a single step that walks the sequence in global order, the same way
    /// <see cref="ReplayObserver"/> already replays one. A step per event source buys a projection no throughput:
    /// one that joins, re-keys or builds a child collection collapses every event source onto a single subscriber
    /// activation, so the steps queue up behind each other anyway - while each of them costs a grain activation,
    /// a prepare and a start, every one of them a storage write. Against a store with tens of thousands of event
    /// sources that burst runs inside the caller's call: Subscribe waits for it, and the client's registration
    /// call waits for Subscribe, until it exceeds its response timeout and takes host startup down with it.
    /// </remarks>
    protected override async Task<IImmutableList<JobStepDetails>> PrepareSteps(CatchUpObserverRequest request)
    {
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        var failedPartitions = await observer.GetFailedPartitionKeys();
        var failedPartitionSet = failedPartitions.ToHashSet();

        var observerKeyIndexes = storage.GetEventStore(JobKey.EventStore).GetNamespace(JobKey.Namespace).ObserverKeyIndexes;
        var index = await observerKeyIndexes.GetFor(request.ObserverKey);
        var keys = index.GetKeys(request.FromEventSequenceNumber);

        var keysToCatchUp = new List<Key>();
        await foreach (var key in keys)
        {
            if (failedPartitionSet.Contains(key))
            {
                continue;
            }

            keysToCatchUp.Add(key);
        }

        // The keys are registered whichever shape the steps take: live delivery is held back per partition while
        // that partition is catching up, and the observer clears the whole set when it routes again afterwards.
        var steps = CreateStepsFor(request, keysToCatchUp);
        await observer.RegisterCatchingUpPartitions(keysToCatchUp);
        return steps;
    }

    ImmutableList<JobStepDetails> CreateStepsFor(CatchUpObserverRequest request, IEnumerable<Key> keys)
    {
        if (request.ObserverType == ObserverType.Projection)
        {
            return
            [
                CreateStep<IHandleEventsForObserver>(
                    new HandleEventsForObserverArguments(
                        request.ObserverKey,
                        request.ObserverType,
                        request.FromEventSequenceNumber,
                        EventSequenceNumber.Max,
                        EventObservationState.None,
                        request.EventTypes)
                    {
                        SkipFailedPartitions = true
                    })
            ];
        }

        return keys
            .Select(key => CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverKey,
                    request.ObserverType,
                    key,
                    request.FromEventSequenceNumber,
                    EventSequenceNumber.Max,
                    EventObservationState.None,
                    request.EventTypes)))
            .ToImmutableList();
    }
}
