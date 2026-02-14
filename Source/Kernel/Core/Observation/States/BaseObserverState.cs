// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.StateMachines;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents a base class for all states of an observer.
/// </summary>
public abstract class BaseObserverState : State<ObserverState>
{
    /// <summary>
    /// Gets the <see cref="ObserverRunningState"/> that the state represents.
    /// </summary>
    public abstract ObserverRunningState RunningState { get; }

    /// <summary>
    /// Gets the <see cref="IObserver"/> the state belongs to.
    /// </summary>
    public IObserver Observer => (StateMachine as IObserver)!;
}
