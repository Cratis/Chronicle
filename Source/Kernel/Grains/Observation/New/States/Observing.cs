// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Orleans.StateMachines;

namespace Aksio.Cratis.Kernel.Grains.Observation.New.States;

/// <summary>
/// Represents the observing state of an observer.
/// </summary>
public class Observing : State<BaseObserverState>
{
    /// <inheritdoc/>
    public override StateName Name => "CatchUp";

    /// <inheritdoc/>
    protected override IImmutableList<Type> SupportedStateTransitions => new[]
    {
        typeof(CatchUp),
        typeof(Replay),
        typeof(Indexing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override Task<BaseObserverState> OnEnter(BaseObserverState state) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override Task<BaseObserverState> OnLeave(BaseObserverState state) => throw new NotImplementedException();
}
