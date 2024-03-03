// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Events;
using Cratis.Kernel.Grains.EventSequences;
using Cratis.Kernel.Storage.Observation;
using Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the subscribing state of an observer.
/// </summary>
public class Routing : BaseObserverState
{
    readonly ObserverKey _observerKey;
    readonly IReplayEvaluator _replayEvaluator;
    readonly IEventSequence _eventSequence;
    readonly ILogger<Routing> _logger;
    ObserverSubscription _subscription;
    EventSequenceNumber _tailEventSequenceNumber = EventSequenceNumber.Unavailable;
    EventSequenceNumber _tailEventSequenceNumberForEventTypes = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="Routing"/> class.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
    /// <param name="replayEvaluator"><see cref="IReplayEvaluator"/> for evaluating replays.</param>
    /// <param name="eventSequence"><see cref="IEventSequence"/> provider.</param>
    /// <param name="logger">Logger for logging.</param>
    public Routing(
        ObserverKey observerKey,
        IReplayEvaluator replayEvaluator,
        IEventSequence eventSequence,
        ILogger<Routing> logger)
    {
        _observerKey = observerKey;
        _replayEvaluator = replayEvaluator;
        _eventSequence = eventSequence;
        _logger = logger;
        _subscription = ObserverSubscription.Unsubscribed;
    }

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
        using var logScope = _logger.BeginRoutingScope(state.ObserverId, _observerKey);
        _subscription = await Observer.GetSubscription();

        _logger.Entering();

        _tailEventSequenceNumber = await _eventSequence.GetTailSequenceNumber();
        _tailEventSequenceNumberForEventTypes = await _eventSequence.GetTailSequenceNumberForEventTypes(_subscription.EventTypes.ToList());

        _logger.TailEventSequenceNumbers(_tailEventSequenceNumber, _tailEventSequenceNumberForEventTypes);

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
            _logger.Indexing();
            await StateMachine.TransitionTo<Indexing>();
            return state;
        }

        if (!_subscription.IsSubscribed)
        {
            _logger.NotSubscribed();
            await StateMachine.TransitionTo<Disconnected>();
            return state;
        }

        if (NeedsToCatchup(state))
        {
            if (CanFastForward(state))
            {
                _logger.FastForwarding();
                state = state with { NextEventSequenceNumber = _tailEventSequenceNumber.Next() };
                return await EvaluateState(state);
            }

            _logger.CatchingUp();
            await StateMachine.TransitionTo<CatchUp>();
        }
        else if (state.RunningState == ObserverRunningState.Replaying)
        {
            _logger.Replaying();
            await StateMachine.TransitionTo<ResumeReplay>();
        }
        else if (await _replayEvaluator.Evaluate(new(
            state.ObserverId,
            _observerKey,
            state,
            _subscription,
            _tailEventSequenceNumber,
            _tailEventSequenceNumberForEventTypes)))
        {
            _logger.NeedsToReplay();
            await StateMachine.TransitionTo<Replay>();
        }
        else
        {
            _logger.Observing();
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
