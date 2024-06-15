// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the subscribing state of an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Routing"/> class.
/// </remarks>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="replayEvaluator"><see cref="IReplayEvaluator"/> for evaluating replays.</param>
/// <param name="eventSequence"><see cref="IEventSequence"/> provider.</param>
/// <param name="logger">Logger for logging.</param>
public class Routing(
    ObserverKey observerKey,
    IReplayEvaluator replayEvaluator,
    IEventSequence eventSequence,
    ILogger<Routing> logger) : BaseObserverState
{
    ObserverSubscription _subscription = ObserverSubscription.Unsubscribed;
    EventSequenceNumber _tailEventSequenceNumber = EventSequenceNumber.Unavailable;
    EventSequenceNumber _tailEventSequenceNumberForEventTypes = EventSequenceNumber.Unavailable;

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Routing;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Disconnected),
        typeof(Indexing),
        typeof(CatchUp),
        typeof(ResumeReplay),
        typeof(Replay),
        typeof(Observing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var logScope = logger.BeginRoutingScope(state.ObserverId, observerKey);
        _subscription = await Observer.GetSubscription();

        logger.Entering();

        _tailEventSequenceNumber = await eventSequence.GetTailSequenceNumber();
        _tailEventSequenceNumberForEventTypes = await eventSequence.GetTailSequenceNumberForEventTypes(_subscription.EventTypes.ToList());

        logger.TailEventSequenceNumbers(_tailEventSequenceNumber, _tailEventSequenceNumberForEventTypes);

        return await EvaluateState(state);
    }

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state)
    {
        return Task.FromResult(state with
        {
            EventTypes = _subscription.IsSubscribed ? _subscription.EventTypes : state.EventTypes,
            NextEventSequenceNumber = _tailEventSequenceNumber.Next(),
            NextEventSequenceNumberForEventTypes = _tailEventSequenceNumberForEventTypes.Next()
        });
    }

    async Task<ObserverState> EvaluateState(ObserverState state)
    {
        if (IsIndexing(state))
        {
            logger.Indexing();
            await StateMachine.TransitionTo<Indexing>();
            return state;
        }

        if (!_subscription.IsSubscribed)
        {
            logger.NotSubscribed();
            await StateMachine.TransitionTo<Disconnected>();
            return state;
        }

        if (NeedsToCatchup(state))
        {
            if (CanFastForward(state))
            {
                logger.FastForwarding();
                state = state with { NextEventSequenceNumber = _tailEventSequenceNumber.Next() };
                return await EvaluateState(state);
            }

            logger.CatchingUp();
            await StateMachine.TransitionTo<CatchUp>();
        }
        else if (state.RunningState == ObserverRunningState.Replaying)
        {
            logger.Replaying();
            await StateMachine.TransitionTo<ResumeReplay>();
        }
        else if (await replayEvaluator.Evaluate(new(
            state.ObserverId,
            observerKey,
            state,
            _subscription,
            _tailEventSequenceNumber,
            _tailEventSequenceNumberForEventTypes)))
        {
            logger.NeedsToReplay();
            await StateMachine.TransitionTo<Replay>();
        }
        else
        {
            logger.Observing();
            await StateMachine.TransitionTo<Observing>();
        }

        return state;
    }

    bool IsIndexing(ObserverState state) => state.RunningState == ObserverRunningState.Indexing;

    bool NeedsToCatchup(ObserverState state) =>
        state.RunningState == ObserverRunningState.CatchingUp ||
        IsFallingBehind(state);

    bool CanFastForward(ObserverState state) =>
        IsFallingBehind(state) &&
        state.NextEventSequenceNumberForEventTypes.IsActualValue &&
        _tailEventSequenceNumberForEventTypes.IsActualValue &&
        _tailEventSequenceNumberForEventTypes < state.NextEventSequenceNumberForEventTypes;

    bool IsFallingBehind(ObserverState state) =>
        state.NextEventSequenceNumber.IsActualValue &&
        _tailEventSequenceNumber.IsActualValue &&
        state.NextEventSequenceNumber < _tailEventSequenceNumber;
}
