// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the command for fast forwarding an observer to the tail of its event sequence.
/// </summary>
/// <param name="EventStore">The event store the observer belongs to.</param>
/// <param name="Namespace">The namespace within the event store the observer belongs to.</param>
/// <param name="ObserverId">The identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer observes.</param>
/// <remarks>
/// All events the observer has not yet handled are skipped without invoking the observer. Use it to get past a
/// backlog, or events that can never be handled, when the read side does not need to be rebuilt.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.Observers)]
public record FastForward(string EventStore, string Namespace, string ObserverId, string EventSequenceId)
{
    /// <summary>
    /// Handles the command by fast forwarding the target observer grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get observer grains with.</param>
    /// <returns>The <see cref="FastForwardResult"/> describing what happened.</returns>
    public Task<FastForwardResult> Handle(IGrainFactory grainFactory)
    {
        var eventSequenceId = string.IsNullOrEmpty(EventSequenceId) ? Concepts.EventSequences.EventSequenceId.Log : (Concepts.EventSequences.EventSequenceId)EventSequenceId;
        var key = new ObserverKey((ObserverId)ObserverId, (EventStoreName)EventStore, (EventStoreNamespaceName)Namespace, eventSequenceId);
        return grainFactory.GetGrain<IObserver>(key).FastForward();
    }
}
