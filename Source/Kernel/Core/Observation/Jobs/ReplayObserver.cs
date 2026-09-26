// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a job for replaying an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReplayObserver"/> class.
/// </remarks>
/// <param name="replayStateServiceClient"><see cref="IObserverServiceClient"/>.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
public class ReplayObserver(
    IObserverServiceClient replayStateServiceClient,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ReplayObserver> logger) : Job<ReplayObserverRequest, JobStateWithLastHandledEvent>, IReplayObserver
{
    /// <inheritdoc/>
    /// <remarks>
    /// A definition change replays through Subscribe, and behind Subscribe sits the client's registration call with
    /// a response timeout on it. A reactor or reducer replays with a step per event source, so bringing those steps
    /// up must not be billed to that call.
    /// </remarks>
    protected override bool StartStepsInBackground => true;

    /// <inheritdoc/>
    protected override async Task<IImmutableList<JobStepDetails>> PrepareSteps(ReplayObserverRequest request)
    {
        var observer = GrainFactory.GetGrain<IObserver>(request.ObserverKey);
        State.ReplayStartedAt = DateTimeOffset.UtcNow;
        State.FailedPartitionKeys = (await observer.GetFailedPartitionKeys()).Distinct().ToList();
        State.ReplayPartitionSteps.Clear();

        if (request.ObserverType == ObserverType.Projection)
        {
            return
            [
                CreateStep<IHandleEventsForObserver>(
                    new HandleEventsForObserverArguments(
                        request.ObserverKey,
                        request.ObserverType,
                        EventSequenceNumber.First,
                        EventSequenceNumber.Max,
                        EventObservationState.Replay,
                        request.EventTypes))
            ];
        }

        var observerKeyIndexes = storage.GetEventStore(JobKey.Scope).GetNamespace(JobKey.Namespace).ObserverKeyIndexes;
        var index = await observerKeyIndexes.GetFor(request.ObserverKey);

        var keys = index.GetKeys(EventSequenceNumber.First);
        var steps = new List<JobStepDetails>();
        var failedKeys = State.FailedPartitionKeys.ToHashSet();

        await foreach (var key in keys)
        {
            var step = CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverKey,
                    request.ObserverType,
                    key,
                    EventSequenceNumber.First,
                    EventSequenceNumber.Max,
                    EventObservationState.Replay,
                    request.EventTypes));
            steps.Add(step);
            if (failedKeys.Contains(key))
            {
                // Only failures present at preparation need coverage tracked in the job state.
                // A step without a recorded successful result (including after a restart before its
                // completion is stored) has no proven watermark and leaves its failure in place.
                State.ReplayPartitionSteps.Add(new(step.Id, key, EventSequenceNumber.Unavailable));
            }
        }

        return steps.ToImmutableList();
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeStartingJobSteps()
    {
        await DeleteAllOtherJobsForObserver();
        await replayStateServiceClient.BeginReplayFor(State.ObserverDetails);
    }

    /// <inheritdoc/>
    protected override Task<bool> CanResume()
    {
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        return observer.IsSubscribed();
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeResumingJobSteps()
    {
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        await observer.Replay();
        await replayStateServiceClient.ResumeReplayFor(State.ObserverDetails);
    }

    /// <inheritdoc/>
    protected override Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result)
    {
        State.HandleResult(result, jsonSerializerOptions);
        if (result.TryGetFullResult<HandleEventsForPartitionResult>(out var handled, out _, jsonSerializerOptions) &&
            handled?.LastHandledEventSequenceNumber.IsActualValue == true)
        {
            for (var index = 0; index < State.ReplayPartitionSteps.Count; index++)
            {
                var step = State.ReplayPartitionSteps[index];
                if (step.Id == jobStepId)
                {
                    State.ReplayPartitionSteps[index] = step with { LastHandledEventSequenceNumber = handled.LastHandledEventSequenceNumber };
                    break;
                }
            }
        }

        var progress = State.Progress;
        if (progress.TotalSteps > 0)
        {
            var completedSteps = progress.SuccessfulSteps + progress.FailedSteps + progress.StoppedSteps;
            var percentComplete = (double)completedSteps / progress.TotalSteps * 100;
            logger.ReplayProgress(completedSteps, progress.TotalSteps, percentComplete, State.LastHandledEventSequenceNumber);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnStopped() => base.OnStopped();

    /// <inheritdoc/>
    protected override async Task OnFailedToPrepare()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        try
        {
            await replayStateServiceClient.EndReplayFor(State.ObserverDetails);
        }
        catch (Exception exception)
        {
            logger.ReplayFinalizationFailed(exception);
        }

        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);

        // Preparing failed; never report a successful replay. Avoid waiting on Replay()'s own turn.
        _ = NotifyObserverOfCompletion(observer, false, new Dictionary<Key, EventSequenceNumber>(), [], EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    protected override async Task OnAllStepsCompleted()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        var finalized = true;
        try
        {
            await replayStateServiceClient.EndReplayFor(State.ObserverDetails);
        }
        catch (Exception exception)
        {
            logger.ReplayFinalizationFailed(exception);
            finalized = false;
        }

        if (!AllStepsCompletedSuccessfully)
        {
            if (State.LastHandledEventSequenceNumber.IsActualValue)
            {
                logger.NotAllEventsWereHandled(nameof(ReplayObserver), State.LastHandledEventSequenceNumber);
            }
            else
            {
                logger.NoEventsWereHandled(nameof(ReplayObserver));
            }
        }

        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);

        var canResolve = finalized && AllStepsCompletedSuccessfully && State.HandledAllEvents && State.LastHandledEventSequenceNumber.IsActualValue;
        Dictionary<Key, EventSequenceNumber> coveredPartitions = [];
        EventType[] eventTypes = [];
        if (canResolve)
        {
            try
            {
                // A projection's single ordered step covers every partition up to its own global watermark.
                // Reactors and reducers have independent steps: only a successful result from that partition counts.
                coveredPartitions = Request.ObserverType == ObserverType.Projection
                    ? State.FailedPartitionKeys.ToDictionary(_ => _, _ => State.LastHandledEventSequenceNumber)
                    : State.ReplayPartitionSteps
                        .Where(_ => _.LastHandledEventSequenceNumber.IsActualValue)
                        .ToDictionary(_ => _.Partition, _ => _.LastHandledEventSequenceNumber);

                // An empty request resolves its event types inside the step at read time. We cannot
                // prove what that step read from the definition at completion, so retain those failures.
                eventTypes = Request.EventTypes.ToArray();
            }
            catch (Exception exception)
            {
                logger.ReplayCompletionNotificationFailed(exception);
                canResolve = false;
            }
        }

        // Do not await from job.Start's turn: Replay() on the observer may still be waiting on us.
        // Observe faults so a failed completion is logged rather than silently reported as success.
        _ = NotifyObserverOfCompletion(observer, canResolve, coveredPartitions, eventTypes, State.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.ObserverKey.ObserverId}";

    async Task NotifyObserverOfCompletion(IObserver observer, bool canResolve, IReadOnlyDictionary<Key, EventSequenceNumber> coveredPartitions, EventType[] eventTypes, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        try
        {
            if (canResolve)
            {
                await observer.ReplayedSuccessfullySince(lastHandledEventSequenceNumber, coveredPartitions, eventTypes, State.ReplayStartedAt);
            }
            else
            {
                await observer.Replayed(lastHandledEventSequenceNumber);
            }
        }
        catch (Exception exception)
        {
            logger.ReplayCompletionNotificationFailed(exception);
        }
    }

    async Task DeleteAllOtherJobsForObserver()
    {
        var observerDetails = State.ObserverDetails;
        var (eventStore, namespaceName) = (observerDetails.Key.EventStore, observerDetails.Key.Namespace);
        var jobsManager = GrainFactory.GetJobsManager(eventStore, namespaceName);
        var jobs = await jobsManager.GetAllJobs();
        jobs = jobs.Where(job => job.Request is IObserverJobRequest observerJobRequest && observerJobRequest.ObserverKey == observerDetails.Key).ToImmutableList();

        var deleteAllOtherJobsForObserver = jobs.Where(job => job.Id != State.Id).Select(job => jobsManager.Delete(job.Id));
        await Task.WhenAll(deleteAllOtherJobsForObserver);
    }
}
