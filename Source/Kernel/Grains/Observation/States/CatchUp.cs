// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the catch up state of an observer.
/// </summary>
public class CatchUp : BaseObserverState
{
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
    public override Task<ObserverState> OnEnter(ObserverState state) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state)
    {
        // Set the last event sequence number to the last event sequence number of the event sequence
        return Task.FromResult(state);
    }
}
