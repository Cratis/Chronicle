// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.New.States;

/// <summary>
/// Represents the replay state of an observer.
/// </summary>
public class Replay : BaseObserverState
{
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
    public override Task<ObserverState> OnEnter(ObserverState state) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state) => throw new NotImplementedException();
}
