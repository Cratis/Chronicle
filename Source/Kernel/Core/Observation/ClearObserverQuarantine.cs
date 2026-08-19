// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the command for taking an observer out of quarantine.
/// </summary>
/// <param name="EventStore">The event store the observer belongs to.</param>
/// <param name="Namespace">The namespace within the event store the observer belongs to.</param>
/// <param name="ObserverId">The identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer observes.</param>
/// <remarks>
/// A quarantined observer never recovers on its own - that is the point of the state. Clearing it is an explicit
/// operator decision, taken once whatever kept failing has been dealt with.
/// </remarks>
[Command]
public record ClearObserverQuarantine(string EventStore, string Namespace, string ObserverId, string EventSequenceId)
{
    /// <summary>
    /// Handles the command by clearing the quarantine on the target observer grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(IGrainFactory grainFactory)
    {
        var eventSequenceId = string.IsNullOrEmpty(EventSequenceId) ? Concepts.EventSequences.EventSequenceId.Log : (Concepts.EventSequences.EventSequenceId)EventSequenceId;
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        return grainFactory.GetGrain<IObserver>(key).ClearObserverQuarantine();
    }
}
