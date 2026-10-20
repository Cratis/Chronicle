// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Monads;
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
    /// <summary>
    /// The number of times the observer is notified that the replay concluded before it is left to the observer's watchdog.
    /// </summary>
    internal const int MaximumNotificationAttempts = 5;

    /// <summary>
    /// The number of partition steps a replay keeps active at once.
    /// </summary>
    /// <remarks>
    /// Every step reports its progress to the one observer grain, which answers one call at a time. A step per
    /// partition started all at once - thousands of them - queued those calls past the response timeout, and every
    /// timeout made another. The window is what that one grain can serve, not what the silos could run.
    /// </remarks>
    internal const int ActiveStepWindow = 16;

    /// <summary>
    /// Gets the notification of the observer that the replay concluded, once the job has concluded.
    /// </summary>
    /// <remarks>
    /// It is not awaited from the job's own turn - the observer's replay may still be waiting on this job - so it is
    /// kept here to be observed instead of being dropped. It retries, and a notification that still cannot be delivered
    /// is left for the observer's watchdog, which finds a replaying observer with no replay job and routes it again.
    /// </remarks>
    internal Task CompletionNotification { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Gets or sets how long to wait before notifying the observer again after it did not take the notification.
    /// </summary>
    internal TimeSpan NotificationRetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <inheritdoc/>
    protected override int? MaxActiveSteps => ActiveStepWindow;

    /// <inheritdoc/>
    /// <remarks>
    /// A definition change replays through Subscribe, and behind Subscribe sits the client's registration call with
    /// a response timeout on it. A reactor or reducer replays with a step per event source, so bringing those steps
    /// up must not be billed to that call.
    /// </remarks>
    protected override bool StartStepsInBackground => true;

    /// <inheritdoc/>
    /// <remarks>
    /// Recovery of a running job can bypass CanResume and go straight to finalization. Check before the base
    /// implementation writes progress or dispatches anything; a scope refusal is terminal, not a resumable stop.
    /// </remarks>
    public new async Task<Result<ResumeJobSuccess, ResumeJobError>> Resume()
    {
        if (State.Status == JobStatus.Failed) return await base.Resume();
        try
        {
            await EnsureReplaySupported();
        }
        catch (GlobalProjectionReplayNotSupported)
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobCannotBeResumed);
        }

        return await base.Resume();
    }

    /// <inheritdoc/>
    protected override async Task<IImmutableList<JobStepDetails>> PrepareSteps(ReplayObserverRequest request)
    {
        await GlobalProjectionReplay.EnsureSupported(storage, request.ObserverKey, request.ObserverType);
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
                        request.EventTypes)
                    {
                        ReaderScope = request.ReaderScope
                    })
            ];
        }

        var eventStoreStorage = storage.GetEventStore(JobKey.Scope);
        var observerKeyIndexes = eventStoreStorage.GetNamespace(JobKey.Namespace).ObserverKeyIndexes;

        // Legacy key enumeration used the stored definition at preparation time, not the request's types.
        var readerScope = request.ReaderScope ?? ObserverReaderScope.For(
            false,
            (await eventStoreStorage.Observers.Get(request.ObserverKey.ObserverId)).EventTypes);
        var index = await observerKeyIndexes.GetFor(request.ObserverKey, readerScope);

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
                    request.EventTypes)
                {
                    ReaderScope = request.ReaderScope
                });
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
        await EnsureReplaySupported();
        await DeleteAllOtherJobsForObserver();
        await replayStateServiceClient.BeginReplayFor(State.ObserverDetails);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A quarantined or disconnected observer cannot enter replay, so resuming beside it would fail only after the job is
    /// recorded as running, leaving it running with no steps for the observer to adopt and wait on forever. Refusing here
    /// keeps the job stopped; the observer resumes it from its own replay entry once it can replay again. Only the
    /// interleaving state query is used - asking the observer to replay from here would wait on it while it may be
    /// resuming this job.
    /// </remarks>
    protected override async Task<bool> CanResume()
    {
        await EnsureReplaySupported();
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        if (!await observer.IsSubscribed())
        {
            return false;
        }

        var observerState = await observer.GetState();
        return observerState.RunningState is not (ObserverRunningState.Quarantined or ObserverRunningState.Disconnected);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The sinks pick their container from replay mode alone, so they must not be switched into replay while the observer
    /// may still be handling live events, or a live write lands in the replay container.
    /// </para>
    /// <para>
    /// The observer resumes a stopped replay job from inside its own Replay transition, and has already marked itself
    /// replaying before doing so. Its turn is waiting on this job, so waiting on it from here would hold both until the
    /// call timed out; the interleaving state query tells that case apart and it is not waited on. Any other resume - an
    /// operator's, or one beside an observer whose replay entry was interrupted - waits for the observer to enter replay
    /// and adopt this job before the sinks are switched and the steps start. That cannot deadlock: the observer then finds
    /// this job already running and does not call back into it. If the observer does not enter replay with this job, the
    /// resume fails rather than run steps beside live handling. <see cref="CanResume"/> already refuses an observer that
    /// cannot replay, so this is left for an observer that changed state in between; the job is then left running with no
    /// steps until the job framework reverts a failed resume (Cratis/Orleans#62).
    /// </para>
    /// </remarks>
    /// <exception cref="ObserverDidNotEnterReplay">The observer did not enter replay with this job.</exception>
    protected override async Task OnBeforeResumingJobSteps()
    {
        await EnsureReplaySupported();
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        var observerState = await observer.GetState();
        if (observerState.RunningState != ObserverRunningState.Replaying)
        {
            var replayJobId = await observer.Replay();
            if (replayJobId != JobId)
            {
                throw new ObserverDidNotEnterReplay(Request.ObserverKey, JobId, replayJobId);
            }
        }

        await replayStateServiceClient.ResumeReplayFor(State.ObserverDetails);
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
    }

    /// <inheritdoc/>
    protected override Task OnStopped() => base.OnStopped();

    /// <inheritdoc/>
    protected override async Task OnFailedToPrepare()
    {
        await EnsureReplaySupported();
        using var scope = logger.BeginJobScope(JobId, JobKey);
        try
        {
            await FinalizeOrAbandon(promote: false);
        }
        catch (Exception exception)
        {
            logger.ReplayFinalizationFailed(exception);
        }

        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);

        // Preparing failed; never report a successful replay. Avoid waiting on Replay()'s own turn.
        CompletionNotification = NotifyObserverOfCompletion(observer, false, new Dictionary<Key, EventSequenceNumber>(), [], EventSequenceNumber.Unavailable);
    }

    /// <inheritdoc/>
    protected override async Task OnAllStepsCompleted()
    {
        await EnsureReplaySupported();
        using var scope = logger.BeginJobScope(JobId, JobKey);
        var finalized = true;
        try
        {
            finalized = await FinalizeOrAbandon(promote: AllStepsCompletedSuccessfully);
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
        // The notification is kept and retried rather than dropped, so a failed one is not silently lost.
        CompletionNotification = NotifyObserverOfCompletion(observer, canResolve, coveredPartitions, eventTypes, State.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.ObserverKey.ObserverId}";

    /// <summary>
    /// Ends the replay on every silo, promoting what it rebuilt only when that is safe.
    /// </summary>
    /// <remarks>
    /// A reducer's replay rebuilds each partition in its own step. If any step failed or stopped, its documents in the
    /// replay container are partial or missing, and promoting the container would replace complete documents with them.
    /// The replay is abandoned instead and the read model keeps the state it had before the replay started; the failed
    /// partitions are recorded on the observer and a later replay starts over. A projection's replay is a single ordered
    /// step whose failed partitions are recorded and retried after promotion, so it is always finalized as before.
    /// </remarks>
    /// <param name="promote">Whether every step completed successfully.</param>
    /// <returns>True if the replay was finalized; false if it was abandoned.</returns>
    async Task<bool> FinalizeOrAbandon(bool promote)
    {
        await EnsureReplaySupported();
        if (!promote && Request.ObserverType == ObserverType.Reducer)
        {
            logger.AbandoningReplay(Request.ObserverKey.ObserverId);
            await replayStateServiceClient.AbandonReplayFor(State.ObserverDetails);
            return false;
        }

        await replayStateServiceClient.EndReplayFor(State.ObserverDetails);
        return true;
    }

    async Task NotifyObserverOfCompletion(IObserver observer, bool canResolve, IReadOnlyDictionary<Key, EventSequenceNumber> coveredPartitions, EventType[] eventTypes, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        for (var attempt = 1; ; attempt++)
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

                return;
            }
            catch (Exception exception)
            {
                logger.ReplayCompletionNotificationFailed(exception);
                if (attempt >= MaximumNotificationAttempts)
                {
                    return;
                }
            }

            await Task.Delay(NotificationRetryDelay);
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

    Task EnsureReplaySupported() => GlobalProjectionReplay.EnsureSupported(
        storage, Request.ObserverKey, Request.ObserverType, State, WriteStateAsync);
}
