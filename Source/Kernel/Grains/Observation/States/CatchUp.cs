// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the catch up state of an observer.
/// </summary>
public class CatchUp : BaseObserverState
{
    readonly IJobsManager _jobsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatchUp"/> class.
    /// </summary>
    /// <param name="jobsManager"><see cref="IJobsManager"/> for working with jobs.</param>
    public CatchUp(IJobsManager jobsManager)
    {
        _jobsManager = jobsManager;
    }

    /// <inheritdoc/>
    public override StateName Name => "CatchUp";

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.CatchingUp;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Replay),
        typeof(Indexing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        await Task.CompletedTask;
        return state;
    }

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state)
    {
        // Set the last event sequence number to the last event sequence number of the event sequence
        return Task.FromResult(state);
    }
}
