// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

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
