// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the subscribing state of an observer.
/// </summary>
public class Subscribing : BaseObserverState
{
    readonly IObserver _observer;
    readonly IEventSequenceStorage _eventSequenceStorage;
    ObserverSubscription _subscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="Subscribing"/> class.
    /// </summary>
    /// <param name="observer"><see cref="IObserver"/> the state belongs to.</param>
    /// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> provider.</param>
    public Subscribing(
        IObserver observer,
        IEventSequenceStorage eventSequenceStorage)
    {
        _observer = observer;
        _eventSequenceStorage = eventSequenceStorage;
        _subscription = ObserverSubscription.Unsubscribed;
    }

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Subscribing;

    /// <inheritdoc/>
    public override StateName Name => "Subscribing";

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        _subscription = await _observer.GetSubscription();
        state.TailEventSequenceNumbers = await _eventSequenceStorage.GetTailSequenceNumbers(state.EventSequenceId, _subscription.EventTypes);

        if (IsIndexing(state))
        {
            await StateMachine.TransitionTo<Indexing>();
        }
        else if (NeedsToCatchup(state))
        {
            await StateMachine.TransitionTo<CatchUp>();
        }
        else if (NeedsToReplay(state))
        {
            await StateMachine.TransitionTo<Replay>();
        }
        else
        {
            await StateMachine.TransitionTo<Observing>();
        }

        state.EventTypes = _subscription.EventTypes;
        state.NextEventSequenceNumber = state.TailEventSequenceNumbers.Tail.Next();

        return state;
    }

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state) => Task.FromResult(state);

    bool IsIndexing(ObserverState state) => state.RunningState == ObserverRunningState.Indexing;

    bool NeedsToCatchup(ObserverState state) =>
        state.RunningState == ObserverRunningState.CatchingUp ||
        IsFallingBehind(state);

    bool IsFallingBehind(ObserverState state) =>
        !state.TailEventSequenceNumbers.TailForEventTypes.IsUnavailable && state.TailEventSequenceNumbers.TailForEventTypes < state.TailEventSequenceNumbers.Tail;

    bool NeedsToReplay(ObserverState state) =>
        state.RunningState == ObserverRunningState.Replaying ||
        (HasDefinitionChanged(state) && HasEventsInSequence(state));

    bool HasEventsInSequence(ObserverState state) =>
        state.TailEventSequenceNumbers.Tail.IsActualValue && state.TailEventSequenceNumbers.TailForEventTypes.IsActualValue;

    bool HasDefinitionChanged(ObserverState state) =>
        state.EventTypes.Count() != _subscription.EventTypes.Count() ||
        !_subscription.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(state.EventTypes.OrderBy(_ => _.Id.Value));
}
