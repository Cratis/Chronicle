// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents the subscribing state of an observer.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="definitionState"><see cref="IPersistentState{ObserverDefinition}"/> for the observer's definition.</param>
/// <param name="eventSequence"><see cref="IEventSequence"/> provider.</param>
/// <param name="storage"><see cref="IStorage"/> used to find out whether catching up would have any work to do.</param>
/// <param name="logger">Logger for logging.</param>
public class Routing(
    ObserverKey observerKey,
    IPersistentState<ObserverDefinition> definitionState,
    IEventSequence eventSequence,
    IStorage storage,
    ILogger<Routing> logger) : BaseObserverState
{
    ObserverSubscription _subscription = ObserverSubscription.Unsubscribed;
    EventSequenceNumber _tailEventSequenceNumber = EventSequenceNumber.Unavailable;
    EventSequenceNumber _nextUnhandledEventSequenceNumber = EventSequenceNumber.Unavailable;

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Unknown;

    /// <summary>
    /// Gets whether routing is waiting for the watchdog to retry a failed event lookup.
    /// </summary>
    internal bool NextEventLookupFailed { get; private set; }

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Routing),
        typeof(Disconnected),
        typeof(Replay),
        typeof(Observing),
        typeof(CatchingUpInFlight)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state)
    {
        if (_subscription.IsSubscribed)
        {
            definitionState.State = definitionState.State with
            {
                EventTypes = _subscription.EventTypes,
            };
        }

        return base.OnLeave(state);
    }

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var logScope = logger.BeginRoutingScope(state.Identifier, observerKey);

        // An in-flight routing pass owns its lookup; only a completed failure owes a watchdog retry.
        NextEventLookupFailed = false;
        _subscription = await Observer.GetSubscription();

        logger.Entering();

        if (state.AlertDisposition == AlertDisposition.Retired)
        {
            await StateMachine.TransitionTo<Disconnected>();
            return state;
        }

        _tailEventSequenceNumber = await eventSequence.GetTailSequenceNumber();
        if (_tailEventSequenceNumber.IsActualValue && (!state.TailEventSequenceNumber.IsActualValue || state.TailEventSequenceNumber < _tailEventSequenceNumber))
        {
            state = state with { TailEventSequenceNumber = _tailEventSequenceNumber };
        }

        // First-time subscription after state reset: start from beginning so existing events are not skipped.
        // NextEventSequenceNumber is Unavailable when the observer grain reloaded state from an empty DB.
        // When the event log is also empty, we still need to prime NextEventSequenceNumber to First so that
        // Handle() does not discard the very first event (sequence 0 >= ulong.MaxValue is false, so the
        // event would be silently dropped if we left the value as Unavailable).
        state = await FindNextUnhandledEvent(state);
        if (NextEventLookupFailed)
        {
            return state;
        }

        logger.TailEventSequenceNumbers(_tailEventSequenceNumber, _nextUnhandledEventSequenceNumber);

        // We should not be having any partitions to catch up or replay at this point, routing will figure out if we need to catch up the observer
        state.CatchingUpPartitions.Clear();
        state.ReplayingPartitions.Clear();

        return await EvaluateState(state);
    }

    async Task<ObserverState> FindNextUnhandledEvent(ObserverState state)
    {
        var nextEventSequenceNumber = FirstPositionToLookFrom(state);
        var getNextToHandleResult = await eventSequence.GetNextSequenceNumberGreaterOrEqualTo(nextEventSequenceNumber, _subscription.EventTypes.ToList());
        _nextUnhandledEventSequenceNumber = getNextToHandleResult.Match(
            eventSequenceNumber => eventSequenceNumber,
            error => OnNextEventLookupError(nextEventSequenceNumber, error));

        return NextEventLookupFailed ? state : state with { NextEventSequenceNumber = nextEventSequenceNumber };
    }

    EventSequenceNumber FirstPositionToLookFrom(ObserverState state) =>
        state.NextEventSequenceNumber.IsActualValue ? state.NextEventSequenceNumber : EventSequenceNumber.First;

    EventSequenceNumber OnNextEventLookupError(EventSequenceNumber nextEventSequenceNumber, GetSequenceNumberError error)
    {
        NextEventLookupFailed = error != GetSequenceNumberError.NotFound;
        if (NextEventLookupFailed)
        {
            logger.NextEventLookupFailed(nextEventSequenceNumber, error);
        }

        return EventSequenceNumber.Unavailable;
    }

    async Task<ObserverState> EvaluateState(ObserverState state)
    {
        if (!_subscription.IsSubscribed)
        {
            logger.NotSubscribed();
            await StateMachine.TransitionTo<Disconnected>();
            return state;
        }

        // An observer subscribed to all events has no fixed event type list on its subscription by design -
        // the whole point is that it also covers event types that do not exist yet - so an empty list there is
        // not a reason to disconnect it the way it would be for an ordinary, explicitly-typed subscription.
        if (!_subscription.EventTypes.Any() && !state.SubscribesToAllEvents)
        {
            logger.NoEventTypes();
            await StateMachine.TransitionTo<Disconnected>();
            return state;
        }

        // Like catch-up, a pending replay waits for a valid subscription before starting work.
        if (state.IsReplaying)
        {
            await StateMachine.TransitionTo<Replay>();
            return state;
        }

        if (CanFastForward(state))
        {
            logger.FastForwarding();
            state = state with { NextEventSequenceNumber = _tailEventSequenceNumber.Next() };
            return await EvaluateState(state);
        }
        if (IsFallingBehind(state) && await HasCatchUpWork(state))
        {
            logger.CatchingUp();
            await Observer.CatchUp();
            await StateMachine.TransitionTo<Observing>();
        }
        else
        {
            logger.Observing();
            if (_tailEventSequenceNumber.IsActualValue)
            {
                state = state with
                {
                    NextEventSequenceNumber = _tailEventSequenceNumber.Next()
                };
            }

            await StateMachine.TransitionTo<Observing>();
        }

        return state;
    }

    /// <summary>
    /// Decides whether a catch-up would have anything to handle.
    /// </summary>
    /// <remarks>
    /// Events of failed partitions are owned by their retry jobs and a catch-up skips them, exactly as
    /// <see cref="Observing.OnEnter"/> stays quiet while the observer has failed partitions. Treating them as
    /// falling behind produces a catch-up with zero work that completes without moving the observer, which
    /// routes again to the same verdict without end. When the key index cannot tell, catch-up is assumed to have work.
    /// </remarks>
    /// <param name="state">The current <see cref="ObserverState"/>.</param>
    /// <returns>True if catching up has events to handle, false otherwise.</returns>
    async Task<bool> HasCatchUpWork(ObserverState state)
    {
        try
        {
            if (!await Observer.HasFailedPartitions())
            {
                return true;
            }

            var failedPartitions = (await Observer.GetFailedPartitionKeys()).ToHashSet();
            var readerScope = ObserverReaderScope.For(state.SubscribesToAllEvents, definitionState.State.EventTypes);
            var index = await storage
                .GetEventStore(observerKey.EventStore)
                .GetNamespace(observerKey.Namespace)
                .ObserverKeyIndexes
                .GetFor(observerKey, readerScope);

            await foreach (var key in index.GetKeys(state.NextEventSequenceNumber))
            {
                if (!failedPartitions.Contains(key))
                {
                    return true;
                }
            }

            logger.OnlyFailedPartitionsBehind();
            return false;
        }
        catch (Exception ex)
        {
            logger.CouldNotDetermineCatchUpWork(ex);
            return true;
        }
    }

    /// <summary>
    /// Decides whether the observer can move its position past the tail without catching up.
    /// </summary>
    /// <remarks>
    /// Only proven absence of an event of the observer's types at or after its position allows that. The last handled
    /// event is the furthest event any partition handled, not a position below which everything was handled: a partition
    /// job, or a catch-up round reporting while a live batch is in progress, moves it ahead of the position while events
    /// of other partitions in between are still unhandled. Fast-forwarding on it silently skipped those events.
    /// </remarks>
    /// <param name="state">The current <see cref="ObserverState"/>.</param>
    /// <returns>True if nothing is left to handle between the position and the tail, false otherwise.</returns>
    bool CanFastForward(ObserverState state) =>
        IsFallingBehind(state) &&
        !_nextUnhandledEventSequenceNumber.IsActualValue;

    bool IsFallingBehind(ObserverState state) =>
        state.NextEventSequenceNumber.IsActualValue &&
        _tailEventSequenceNumber.IsActualValue &&
        state.NextEventSequenceNumber <= _tailEventSequenceNumber;
}
