// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts between <see cref="EventToAppend"/> and its contract representation.
/// </summary>
internal static class EventToAppendConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.Sequences.EventToAppend"/> to an <see cref="EventToAppend"/>.
    /// </summary>
    /// <param name="eventToAppend">The contract event to convert.</param>
    /// <returns>The converted event.</returns>
    public static EventToAppend ToApi(this Contracts.Sequences.EventToAppend eventToAppend) =>
        new(eventToAppend.EventType.ToApi(), eventToAppend.Content, eventToAppend.Subject);

    /// <summary>
    /// Converts a named-tag batch event from the generated contract.
    /// </summary>
    /// <param name="eventToAppend">The contract event.</param>
    /// <returns>The converted event.</returns>
    public static EventToAppendWithNamedTags ToApi(this Contracts.Sequences.EventToAppendWithNamedTags eventToAppend) =>
        new(eventToAppend.EventType.ToApi(), eventToAppend.Content, eventToAppend.NamedTags.Select(tag => tag.ToApi()), eventToAppend.Subject);

    /// <summary>
    /// Converts a named-tag event source event from the generated contract.
    /// </summary>
    /// <param name="event">The contract event.</param>
    /// <returns>The converted event.</returns>
    public static EventForEventSourceIdWithNamedTags ToApi(this Contracts.Sequences.EventForEventSourceIdWithNamedTags @event) =>
        new(
            @event.EventSourceId,
            @event.EventSourceType,
            @event.EventStreamType,
            @event.EventStreamId,
            @event.EventType.ToApi(),
            @event.Content,
            @event.Tags,
            @event.NamedTags.Select(tag => tag.ToApi()),
            @event.Occurred,
            @event.Subject);

    /// <summary>
    /// Converts a contract <see cref="Contracts.Sequences.EventForEventSourceId"/> to an <see cref="EventForEventSourceId"/>.
    /// </summary>
    /// <param name="event">The contract event to convert.</param>
    /// <returns>The converted event.</returns>
    public static EventForEventSourceId ToApi(this Contracts.Sequences.EventForEventSourceId @event) =>
        new(
            @event.EventSourceId,
            @event.EventSourceType,
            @event.EventStreamType,
            @event.EventStreamId,
            @event.EventType.ToApi(),
            @event.Content,
            @event.Tags,
            @event.Occurred,
            @event.Subject,
            @event.Causation?.ToApi());
}
