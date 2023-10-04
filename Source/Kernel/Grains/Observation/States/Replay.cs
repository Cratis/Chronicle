// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
    readonly IJobsManager _jobsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUp"/> class.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
    /// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
    public Replay(ObserverKey observerKey, IJobsManager jobsManager)
    {
        _observerKey = observerKey;
        _jobsManager = jobsManager;
    }

    /// <inheritdoc/>
    public override StateName Name => "Replaying";

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Replaying;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Indexing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        await _jobsManager.Start<IReplayJob, ReplayRequest>(
            JobId.New(),
            new ReplayRequest(
                state.ObserverId,
                _observerKey,
                state.EventTypes));

        return state;
    }

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state)
    {
        // Set the last event sequence number to the last event sequence number of the event sequence
        return Task.FromResult(state);
    }
}
