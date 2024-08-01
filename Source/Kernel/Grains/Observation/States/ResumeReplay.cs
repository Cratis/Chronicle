// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the state for resuming replay of an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CatchUp"/> class.
/// </remarks>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="replayStateServiceClient"><see cref="IObserverServiceClient"/> for notifying about replay to all silos.</param>
/// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
public class ResumeReplay(
    ObserverKey observerKey,
    IObserverServiceClient replayStateServiceClient,
    IJobsManager jobsManager) : BaseObserverState
{
    readonly IJobsManager _jobsManager = jobsManager;
    bool _replayStarted;

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Replaying;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Indexing),
        typeof(Routing),
        typeof(Disconnected)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        var subscription = await Observer.GetSubscription();

        var jobs = await _jobsManager.GetJobsOfType<IReplayObserver, ReplayObserverRequest>();
        var jobsForThisObserver = jobs.Where(IsJobForThisObserver);
        if (jobsForThisObserver.Any(_ => _.Status == JobStatus.Running))
        {
            return state;
        }

        _replayStarted = true;
        await replayStateServiceClient.BeginReplayFor(new(state.ObserverId, observerKey, state.Type));

        var pausedJob = jobsForThisObserver.FirstOrDefault(_ => _.Status == JobStatus.Paused);
        if (pausedJob is not null)
        {
            await _jobsManager.Resume(pausedJob.Id);
        }

        return state;
    }

    /// <inheritdoc/>
    public override async Task<ObserverState> OnLeave(ObserverState state)
    {
        if (_replayStarted)
        {
            await replayStateServiceClient.EndReplayFor(new(state.ObserverId, observerKey, state.Type));
            _replayStarted = false;
        }
        return state;
    }

    bool IsJobForThisObserver(JobState jobState) =>
        ((ReplayObserverRequest)jobState.Request).ObserverKey == observerKey;
}
