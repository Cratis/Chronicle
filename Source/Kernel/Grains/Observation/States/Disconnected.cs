// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the disconnected state of an observer.
/// </summary>
public class Disconnected : BaseObserverState
{
    /// <inheritdoc/>
    public override StateName Name => "Disconnected";

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Disconnected;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Routing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override Task<ObserverState> OnEnter(ObserverState state) => Task.FromResult(state);

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state) => Task.FromResult(state);
}
