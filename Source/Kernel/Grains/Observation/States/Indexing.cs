// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.Observation;

namespace Cratis.Chronicle.Grains.Observation.States;

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
