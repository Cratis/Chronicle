// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.States;

/// <summary>
/// Represents the subscribing state of an observer.
/// </summary>
public class Routing : BaseObserverState
{
    readonly ObserverKey _observerKey;
    readonly IObserver _observer;
    readonly IEventSequence _eventSequence;
    readonly ILogger<Routing> _logger;
    ObserverSubscription _subscription;
    EventSequenceNumber _tailEventSequenceNumber = EventSequenceNumber.Unavailable;
    EventSequenceNumber _tailEventSequenceNumberForEventTypes = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="Routing"/> class.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
    /// <param name="observer"><see cref="IObserver"/> the state belongs to.</param>
    /// <param name="eventSequence"><see cref="IEventSequenceStorage"/> provider.</param>
    /// <param name="logger">Logger for logging.</param>
    public Routing(
        ObserverKey observerKey,
        IObserver observer,
        IEventSequence eventSequence,
        ILogger<Routing> logger)
    {
        _observerKey = observerKey;
        _observer = observer;
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
        _subscription = await _observer.GetSubscription();
        using var logScope = _logger.BeginRoutingScope(state.ObserverId, _observerKey);

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
        else if (NeedsToReplay(state))
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

    bool NeedsToReplay(ObserverState state) =>
        HasDefinitionChanged(state) && HasEventsInSequence();

    bool HasEventsInSequence() =>
        _tailEventSequenceNumber.IsActualValue && _tailEventSequenceNumberForEventTypes.IsActualValue;

    bool HasDefinitionChanged(ObserverState state) =>
        state.EventTypes.Count() != _subscription.EventTypes.Count() ||
        !_subscription.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(state.EventTypes.OrderBy(_ => _.Id.Value));
}
