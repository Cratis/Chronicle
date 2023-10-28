// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the subscribing state of an observer.
/// </summary>
public class Routing : BaseObserverState
{
    readonly IObserver _observer;
    readonly IEventSequence _eventSequence;
    ObserverSubscription _subscription;
    EventSequenceNumber _tailEventSequenceNumber = EventSequenceNumber.Unavailable;
    EventSequenceNumber _tailEventSequenceNumberForEventTypes = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="Routing"/> class.
    /// </summary>
    /// <param name="observer"><see cref="IObserver"/> the state belongs to.</param>
    /// <param name="eventSequence"><see cref="IEventSequenceStorage"/> provider.</param>
    public Routing(
        IObserver observer,
        IEventSequence eventSequence)
    {
        _observer = observer;
        _eventSequence = eventSequence;
        _subscription = ObserverSubscription.Unsubscribed;
    }

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Routing;

    /// <inheritdoc/>
    public override StateName Name => "Routing";

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Indexing),
        typeof(CatchUp),
        typeof(Replay),
        typeof(Observing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        _subscription = await _observer.GetSubscription();
        _tailEventSequenceNumber = await _eventSequence.GetTailSequenceNumber();
        _tailEventSequenceNumberForEventTypes = await _eventSequence.GetTailSequenceNumberForEventTypes(_subscription.EventTypes);

        if (!_subscription.IsSubscribed)
        {
            await _observer.TransitionTo<Disconnected>();
        }
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

        return state;
    }

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state)
    {
        return Task.FromResult(state with
        {
            EventTypes = _subscription.EventTypes,
            NextEventSequenceNumber = _tailEventSequenceNumber.Next(),
            NextEventSequenceNumberForEventTypes = _tailEventSequenceNumberForEventTypes.Next()
        });
    }

    bool IsIndexing(ObserverState state) => state.RunningState == ObserverRunningState.Indexing;

    bool NeedsToCatchup(ObserverState state) =>
        state.RunningState == ObserverRunningState.CatchingUp ||
        IsFallingBehind();

    bool IsFallingBehind() =>
        !_tailEventSequenceNumberForEventTypes.IsUnavailable && _tailEventSequenceNumberForEventTypes < _tailEventSequenceNumber;

    bool NeedsToReplay(ObserverState state) =>
        state.RunningState == ObserverRunningState.Replaying ||
        (HasDefinitionChanged(state) && HasEventsInSequence());

    bool HasEventsInSequence() =>
        _tailEventSequenceNumber.IsActualValue && _tailEventSequenceNumberForEventTypes.IsActualValue;

    bool HasDefinitionChanged(ObserverState state) =>
        state.EventTypes.Count() != _subscription.EventTypes.Count() ||
        !_subscription.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(state.EventTypes.OrderBy(_ => _.Id.Value));
}
