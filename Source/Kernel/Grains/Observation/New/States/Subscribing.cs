// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Orleans.StateMachines;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.New.States;

/// <summary>
/// Represents the subscribing state of an observer.
/// </summary>
public class Subscribing : BaseObserverState
{
    readonly IEventSequenceStorage _eventSequenceStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="Subscribing"/> class.
    /// </summary>
    /// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> provider.</param>
    public Subscribing(IEventSequenceStorage eventSequenceStorage)
    {
        _eventSequenceStorage = eventSequenceStorage;
    }

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Subscribing;

    /// <inheritdoc/>
    public override StateName Name => "Subscribing";

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        state.TailEventSequenceNumbers = await _eventSequenceStorage.GetTailSequenceNumbers(state.EventSequenceId, state.Subscription.EventTypes);

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

        state.EventTypes = state.Subscription.EventTypes;
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
        state.EventTypes.Count() != state.Subscription.EventTypes.Count() ||
        !state.Subscription.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(state.EventTypes.OrderBy(_ => _.Id.Value));
}
