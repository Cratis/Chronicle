// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// The pinned public event shape.
/// </summary>
/// <param name="Context">The public event context.</param>
/// <param name="Content">The delivered content.</param>
public record DeliveredEvent(
    [property: JsonPropertyName("context")] DeliveredEventContext Context,
    [property: JsonPropertyName("content")] ExpandoObject Content)
{
    /// <summary>
    /// Maps an event without leaking storage metadata.
    /// </summary>
    /// <param name="event">The event to deliver.</param>
    /// <param name="dedupKey">The canonical event key.</param>
    /// <returns>The public event.</returns>
    public static DeliveredEvent From(AppendedEvent @event, EventDedupKey dedupKey)
    {
        var context = @event.Context;
        return new(new(context.SequenceNumber.Value, new(context.EventType.Id.Value, context.EventType.Generation.Value), context.EventSourceType, context.EventSourceId, context.EventStreamType, context.EventStreamId, context.Occurred, context.CorrelationId, context.Causation, context.CausedBy, context.Tags, context.Subject, context.ObservationState, dedupKey), @event.Content);
    }
}
