// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the disconnected state of an observer.
/// </summary>
public class Disconnected : BaseObserverState
{
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
