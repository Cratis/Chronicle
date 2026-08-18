// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.StateMachines;
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
    /// Gets a value indicating whether the state is a transitional one - a state the observer only passes through
    /// on its way to a settled state, rather than one it rests in.
    /// </summary>
    /// <remarks>
    /// A transitional state has no meaningful <see cref="RunningState"/> to report; it answers
    /// <see cref="ObserverRunningState.Unknown"/> because the observer is between running states, not in one.
    /// Consumers of the observer's reported running state must never see that value, so the observer keeps
    /// reporting the last settled one while it passes through.
    /// </remarks>
    public virtual bool IsTransitional => false;

    /// <summary>
    /// Gets the <see cref="IObserver"/> the state belongs to.
    /// </summary>
    public IObserver Observer => (StateMachine as IObserver)!;
}
