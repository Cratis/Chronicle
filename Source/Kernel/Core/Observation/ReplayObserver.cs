// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the command for replaying an observer from the beginning of its event sequence.
/// </summary>
/// <param name="EventStore">The name of the event store.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="ObserverId">The unique identifier of the observer to replay.</param>
/// <param name="EventSequenceId">The identifier of the event sequence; defaults to the event log when empty.</param>
[Command]
public record ReplayObserver(string EventStore, string Namespace, string ObserverId, string EventSequenceId)
{
    /// <summary>
    /// Handles the command by invoking <see cref="IObserver.Replay"/> on the target observer grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IGrainFactory grainFactory)
    {
        var eventSequenceId = string.IsNullOrEmpty(EventSequenceId) ? Concepts.EventSequences.EventSequenceId.Log : (Concepts.EventSequences.EventSequenceId)EventSequenceId;
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        return grainFactory.GetGrain<IObserver>(key).Replay();
    }
}
