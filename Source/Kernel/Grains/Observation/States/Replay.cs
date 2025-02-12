// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the replay state of an observer.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="replayStateServiceClient"><see cref="IObserverServiceClient"/> for notifying about replay to all silos.</param>
/// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
/// <param name="logger">Logger for logging.</param>
public class Replay(
    ObserverKey observerKey,
    IObserverServiceClient replayStateServiceClient,
    IJobsManager jobsManager,
    ILogger<Replay> logger) : BaseObserverState
{
    bool _replayStarted;

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Replaying;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Routing),
        typeof(Disconnected)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = logger.BeginReplayScope(state.Id, observerKey);

        logger.Entering();

        var subscription = await Observer.GetSubscription();

        var jobs = await jobsManager.GetJobsOfType<IReplayObserver, ReplayObserverRequest>();
        var jobsForThisObserver = jobs.Where(IsJobForThisObserver);
        if (jobsForThisObserver.Any(_ => _.Status == JobStatus.Running))
        {
            logger.FinishingExistingReplayJob();
            return state;
        }

        _replayStarted = true;
        await replayStateServiceClient.BeginReplayFor(new(state.Id, observerKey, state.Type));

        var pausedJob = jobsForThisObserver.FirstOrDefault(_ => _.Status == JobStatus.Paused);
        if (pausedJob is not null)
        {
            logger.ResumingReplayJob();
            await jobsManager.Resume(pausedJob.Id);
            return state;
        }

        logger.StartReplayJob();
        await jobsManager.Start<IReplayObserver, ReplayObserverRequest>(
            JobId.New(),
            new ReplayObserverRequest(
                observerKey,
                subscription,
                state.EventTypes));
        return state;
    }

    /// <inheritdoc/>
    public override async Task<ObserverState> OnLeave(ObserverState state)
    {
        if (!_replayStarted) return state;
        await replayStateServiceClient.EndReplayFor(new(state.Id, observerKey, state.Type));
        _replayStarted = false;
        return state;
    }

    bool IsJobForThisObserver(JobState jobState) =>
        ((ReplayObserverRequest)jobState.Request).ObserverKey == observerKey;
}
