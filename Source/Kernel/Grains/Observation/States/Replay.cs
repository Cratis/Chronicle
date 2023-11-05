// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Grains.Observation.Jobs;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the replay state of an observer.
/// </summary>
public class Replay : BaseObserverState
{
    readonly ObserverKey _observerKey;
    readonly IObserverServiceClient _replayStateServiceClient;
    readonly IJobsManager _jobsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUp"/> class.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
    /// <param name="replayStateServiceClient"><see cref="IObserverServiceClient"/> for notifying about replay to all silos.</param>
    /// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
    public Replay(
        ObserverKey observerKey,
        IObserverServiceClient replayStateServiceClient,
        IJobsManager jobsManager)
    {
        _observerKey = observerKey;
        _replayStateServiceClient = replayStateServiceClient;
        _jobsManager = jobsManager;
    }

    /// <inheritdoc/>
    public override StateName Name => "Replaying";

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
        var jobsForThisObserver = jobs.Where(_ => _.Request.ObserverId == state.ObserverId && _.Request.ObserverKey == _observerKey);
        if (jobsForThisObserver.Any(_ => _.Status == JobStatus.Running))
        {
            return state;
        }

        await _replayStateServiceClient.BeginReplayFor(new(state.ObserverId, _observerKey, state.Type));

        var pausedJob = jobsForThisObserver.FirstOrDefault(_ => _.Status == JobStatus.Paused);
        if (pausedJob is not null)
        {
            await _jobsManager.Resume(pausedJob.Id);
        }
        else
        {
            await _jobsManager.Start<IReplayObserver, ReplayObserverRequest>(
                JobId.New(),
                new ReplayObserverRequest(
                    state.ObserverId,
                    _observerKey,
                    subscription,
                    state.EventTypes));
        }

        return state with { Handled = EventCount.Zero };
    }

    /// <inheritdoc/>
    public override async Task<ObserverState> OnLeave(ObserverState state)
    {
        await _replayStateServiceClient.EndReplayFor(new(state.ObserverId, _observerKey, state.Type));
        return state;
    }
}
