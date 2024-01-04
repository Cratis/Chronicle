// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the state for resuming replay of an observer.
/// </summary>
public class ResumeReplay : BaseObserverState
{
    readonly ObserverId _observerId;
    readonly ObserverKey _observerKey;
    readonly IObserverServiceClient _replayStateServiceClient;
    readonly IJobsManager _jobsManager;
    bool _replayStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUp"/> class.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> for the observer.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
    /// <param name="replayStateServiceClient"><see cref="IObserverServiceClient"/> for notifying about replay to all silos.</param>
    /// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
    public ResumeReplay(
        ObserverId observerId,
        ObserverKey observerKey,
        IObserverServiceClient replayStateServiceClient,
        IJobsManager jobsManager)
    {
        _observerId = observerId;
        _observerKey = observerKey;
        _replayStateServiceClient = replayStateServiceClient;
        _jobsManager = jobsManager;
    }

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
        await _replayStateServiceClient.BeginReplayFor(new(state.ObserverId, _observerKey, state.Type));

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
            await _replayStateServiceClient.EndReplayFor(new(state.ObserverId, _observerKey, state.Type));
            _replayStarted = false;
        }
        return state;
    }

    bool IsJobForThisObserver(JobState jobState) =>
        ((ReplayObserverRequest)jobState.Request).ObserverId == _observerId &&
        ((ReplayObserverRequest)jobState.Request).ObserverKey == _observerKey;
}
