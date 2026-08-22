// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for completing a stream so that no further events can be appended to it.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence holding the stream.</param>
/// <param name="EventStreamType">The stream type to complete.</param>
/// <param name="EventStreamId">The stream within the stream type to complete.</param>
/// <remarks>
/// Handle() still throws for a business rejection (already completed, or the default stream) rather than
/// returning a result - the more correct shape used for <c>Append</c>/<c>AppendMany</c>. Wiring that up would
/// require <see cref="Cratis.Chronicle.EventSequences.CompleteStreamError"/> to become a generator-mirrored shared
/// type, and it mirrors by simple type name into <c>Contracts.EventSequences</c> - the exact path the still-active,
/// hand-written <c>Contracts.EventSequences.CompleteStreamError</c> already occupies for the old service. Mirroring
/// it renumbers that enum's wire values out from under every client still talking to the old service. Revisit once
/// that service is deleted and the collision is moot.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record CompleteStream(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    string EventStreamType,
    string EventStreamId)
{
    /// <summary>
    /// Handles the command by completing the stream.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to complete the stream through.</param>
    /// <returns>The tail <see cref="EventSequenceNumber"/> at the moment of completion.</returns>
    /// <exception cref="StreamCannotBeCompleted">Thrown when the stream cannot be completed.</exception>
    internal async Task<EventSequenceNumber> Handle(IGrainFactory grainFactory)
    {
        var eventSequence = grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace);
        var result = await eventSequence.CompleteStream(EventStreamType, EventStreamId);

        return result.TryGetError(out var error) ? throw new StreamCannotBeCompleted(error) : result.AsT0;
    }
}
