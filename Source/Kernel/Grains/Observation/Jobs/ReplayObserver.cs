// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents a job for replaying an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReplayObserver"/> class.
/// </remarks>
/// <param name="replayStateServiceClient"><see cref="IObserverServiceClient"/>.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="logger">The logger.</param>
public class ReplayObserver(
    IObserverServiceClient replayStateServiceClient,
    IStorage storage,
    ILogger<ReplayObserver> logger) : Job<ReplayObserverRequest, JobStateWithLastHandledEvent>, IReplayObserver
{
    /// <inheritdoc/>
    protected override async Task<IImmutableList<JobStepDetails>> PrepareSteps(ReplayObserverRequest request)
    {
        var observerKeyIndexes = storage.GetEventStore(JobKey.EventStore).GetNamespace(JobKey.Namespace).ObserverKeyIndexes;
        var index = await observerKeyIndexes.GetFor(request.ObserverKey);

        var keys = index.GetKeys(EventSequenceNumber.First);
        var steps = new List<JobStepDetails>();

        await foreach (var key in keys)
        {
            steps.Add(CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverKey,
                    request.ObserverType,
                    key,
                    EventSequenceNumber.First,
                    EventSequenceNumber.Max,
                    EventObservationState.Replay,
                    request.EventTypes)));
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
        State.HandleResult(result);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnStopped() => base.OnStopped();

    /// <inheritdoc/>
    protected override async Task OnFailedToPrepare()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        await replayStateServiceClient.EndReplayFor(State.ObserverDetails);
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        await observer.Replayed(EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    protected override async Task OnCompleted()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        await replayStateServiceClient.EndReplayFor(State.ObserverDetails);
        if (!AllStepsCompletedSuccessfully)
        {
            if (State.LastHandledEventSequenceNumber.IsActualValue)
            {
                logger.NotAllEventsWereHandled(nameof(ReplayObserver), State.LastHandledEventSequenceNumber);
            }
            else
            {
                logger.NoneEventsWereHandled(nameof(ReplayObserver));
            }
        }

        // TODO: Do we need to do anything special if any replaying partitions failed?
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        await observer.Replayed(State.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.ObserverKey.ObserverId}";

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
