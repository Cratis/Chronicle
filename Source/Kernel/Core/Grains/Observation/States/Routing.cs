// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the subscribing state of an observer.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="definitionState"><see cref="IPersistentState{ObserverDefinition}"/> for the observer's definition.</param>
/// <param name="eventSequence"><see cref="IEventSequence"/> provider.</param>
/// <param name="logger">Logger for logging.</param>
public class Routing(
    ObserverKey observerKey,
    IPersistentState<ObserverDefinition> definitionState,
    IEventSequence eventSequence,
    ILogger<Routing> logger) : BaseObserverState
{
    ObserverSubscription _subscription = ObserverSubscription.Unsubscribed;
    EventSequenceNumber _tailEventSequenceNumber = EventSequenceNumber.Unavailable;
    EventSequenceNumber _nextUnhandledEventSequenceNumber = EventSequenceNumber.Unavailable;

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Unknown;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Disconnected),
        typeof(Replay),
        typeof(Observing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state)
    {
        definitionState.State = definitionState.State with
        {
            EventTypes = _subscription.EventTypes,
        };

        return base.OnLeave(state);
    }

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var logScope = logger.BeginRoutingScope(state.Identifier, observerKey);
        _subscription = await Observer.GetSubscription();

        logger.Entering();

        if (observerKey.ObserverId == "Core.Simulations.Dashboard.SimulationDashboard")
        {
            Console.WriteLine("Hello");
        }

        _tailEventSequenceNumber = await eventSequence.GetTailSequenceNumber();
        var getNextToHandleResult = await eventSequence.GetNextSequenceNumberGreaterOrEqualTo(state.NextEventSequenceNumber, _subscription.EventTypes.ToList());
        _nextUnhandledEventSequenceNumber = getNextToHandleResult.Match(eventSequenceNumber => eventSequenceNumber, _ => EventSequenceNumber.Unavailable);

        logger.TailEventSequenceNumbers(_tailEventSequenceNumber, _nextUnhandledEventSequenceNumber);

        // We should not be having any partitions to catch up or replay at this point, routing will figure out if we need to catch up the observer
        state.CatchingUpPartitions.Clear();
        state.ReplayingPartitions.Clear();

        return await EvaluateState(state);
    }

    async Task<ObserverState> EvaluateState(ObserverState state)
    {
        if (state.IsReplaying)
        {
            await StateMachine.TransitionTo<Replay>();
            return state;
        }

        if (!_subscription.IsSubscribed)
        {
            logger.NotSubscribed();
            await StateMachine.TransitionTo<Disconnected>();
            return state;
        }

        if (!_subscription.EventTypes.Any())
        {
            logger.NoEventTypes();
            await StateMachine.TransitionTo<Disconnected>();
            return state;
        }

        if (CanFastForward(state))
        {
            logger.FastForwarding();
            state = state with { NextEventSequenceNumber = _tailEventSequenceNumber.Next() };
            return await EvaluateState(state);
        }
        if (IsFallingBehind(state))
        {
            logger.CatchingUp();
            await Observer.CatchUp();
            await StateMachine.TransitionTo<Observing>();
        }
        else
        {
            logger.Observing();
            state = state with
            {
                NextEventSequenceNumber = _tailEventSequenceNumber.Next()
            };

            await StateMachine.TransitionTo<Observing>();
        }

        return state;
    }

    bool CanFastForward(ObserverState state) =>
        IsFallingBehind(state) &&
        (!_nextUnhandledEventSequenceNumber.IsActualValue ||
        (_nextUnhandledEventSequenceNumber < state.LastHandledEventSequenceNumber && state.LastHandledEventSequenceNumber.IsActualValue));

    bool IsFallingBehind(ObserverState state) =>
        state.NextEventSequenceNumber.IsActualValue &&
        _tailEventSequenceNumber.IsActualValue &&
        state.NextEventSequenceNumber <= _tailEventSequenceNumber;
}
