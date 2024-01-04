// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage.Observation;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the indexing state of an observer.
/// </summary>
public class Indexing : BaseObserverState
{
    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Indexing;

    /// <inheritdoc/>
    public override Task<ObserverState> OnEnter(ObserverState state) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state) => throw new NotImplementedException();
}
