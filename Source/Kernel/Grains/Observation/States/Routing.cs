// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the subscribing state of an observer.
/// </summary>
public class Routing : BaseObserverState
{
    readonly IObserver _observer;
    readonly IEventSequenceStorage _eventSequenceStorage;
    ObserverSubscription _subscription;
    TailEventSequenceNumbers _tailEventSequenceNumbers = TailEventSequenceNumbers.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Routing"/> class.
    /// </summary>
    /// <param name="observer"><see cref="IObserver"/> the state belongs to.</param>
    /// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> provider.</param>
    public Routing(
        IObserver observer,
        IEventSequenceStorage eventSequenceStorage)
    {
        _observer = observer;
        _eventSequenceStorage = eventSequenceStorage;
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
        _tailEventSequenceNumbers = await _eventSequenceStorage.GetTailSequenceNumbers(state.EventSequenceId, _subscription.EventTypes);

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
        state.EventTypes = _subscription.EventTypes;
        state.NextEventSequenceNumber = _tailEventSequenceNumbers.Tail.Next();
        state.NextEventSequenceNumberForEventTypes = _tailEventSequenceNumbers.TailForEventTypes.Next();

        return Task.FromResult(state);
    }

    bool IsIndexing(ObserverState state) => state.RunningState == ObserverRunningState.Indexing;

    bool NeedsToCatchup(ObserverState state) =>
        state.RunningState == ObserverRunningState.CatchingUp ||
        IsFallingBehind();

    bool IsFallingBehind() =>
        !_tailEventSequenceNumbers.TailForEventTypes.IsUnavailable && _tailEventSequenceNumbers.TailForEventTypes < _tailEventSequenceNumbers.Tail;

    bool NeedsToReplay(ObserverState state) =>
        state.RunningState == ObserverRunningState.Replaying ||
        (HasDefinitionChanged(state) && HasEventsInSequence());

    bool HasEventsInSequence() =>
        _tailEventSequenceNumbers.Tail.IsActualValue && _tailEventSequenceNumbers.TailForEventTypes.IsActualValue;

    bool HasDefinitionChanged(ObserverState state) =>
        state.EventTypes.Count() != _subscription.EventTypes.Count() ||
        !_subscription.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(state.EventTypes.OrderBy(_ => _.Id.Value));
}
