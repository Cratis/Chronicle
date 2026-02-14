// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents the observing state of an observer.
/// </summary>
/// <param name="appendedEventsQueues"><see cref="IAppendedEventsQueue"/> for the observer.</param>
/// <param name="eventStore"><see cref="EventStoreName"/> the state is for.</param>
/// <param name="namespace"><see cref="EventStoreNamespaceName"/> the state is for.</param>
/// <param name="eventSequenceId"><see cref="EventSequenceId"/> being observed.</param>
/// <param name="definitionState">Persistent state <see cref="ObserverDefinition"/> for the observer.</param>
/// <param name="logger">Logger for logging.</param>
public class Observing(
    IAppendedEventsQueues appendedEventsQueues,
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId,
    IPersistentState<ObserverDefinition> definitionState,
    ILogger<Observing> logger) : BaseObserverState
{
    AppendedEventsQueueSubscription? _subscription;

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Active;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Routing),
        typeof(Replay),
        typeof(Disconnected)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = logger.BeginObservingScope(state, eventStore, @namespace, eventSequenceId);
        logger.Entering();

        logger.SubscribingToStream(state.NextEventSequenceNumber);

        var key = new ObserverKey(state.Identifier, eventStore, @namespace, eventSequenceId);
        _subscription = await appendedEventsQueues.Subscribe(key, definitionState.State.EventTypes);

        return state;
    }

    /// <inheritdoc/>
    public override async Task<ObserverState> OnLeave(ObserverState state)
    {
        if (_subscription is not null)
        {
            await appendedEventsQueues.Unsubscribe(_subscription!);
        }
        return state;
    }
}
