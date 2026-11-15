// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Keeps what a projection computes from <c language="json">date-time</c> values the same as before their offsets were kept.
/// </summary>
/// <remarks>
/// A <c language="json">date-time</c> value with an explicit offset is held as a <see cref="DateTimeOffset"/>, so every path that reads
/// an event gives it back with its own offset. Projections computed such a value from the host's local time, and turn a
/// <see cref="DateTimeOffset"/> into its UTC time, which would change the dates, times and text of existing read models
/// and keys. Events therefore enter a projection with their <c language="json">date-time</c> values in the host's local time, as they
/// were held before. Values of the <c language="json">date-time-offset</c> format are left as they are.
/// </remarks>
/// <param name="eventTypeSchemas">The schemas of the event types the projection can receive.</param>
public sealed class ProjectionDateTimes(IEnumerable<EventTypeSchema> eventTypeSchemas)
{
    /// <summary>
    /// The instance that leaves every event as it is.
    /// </summary>
    public static readonly ProjectionDateTimes None = new([]);

    readonly EventTypeSchema[] _eventTypeSchemas = [.. eventTypeSchemas];
    readonly ConcurrentDictionary<EventType, JsonSchema?> _schemaByEventType = new();

    /// <summary>
    /// Gets an event with its <c language="json">date-time</c> values held the way a projection computes from them.
    /// </summary>
    /// <param name="event">The event as read.</param>
    /// <returns>The event to project; the same instance when nothing changes.</returns>
    public AppendedEvent ForProjection(AppendedEvent @event)
    {
        if (_eventTypeSchemas.Length == 0 || SchemaFor(@event.Context.EventType) is not { } schema)
        {
            return @event;
        }

        var content = DateTimeValues.AsHeldBeforeOffsetsWereKept(@event.Content, schema);
        return ReferenceEquals(content, @event.Content) ? @event : @event with { Content = content };
    }

    JsonSchema? SchemaFor(EventType eventType) =>
        _schemaByEventType.GetOrAdd(eventType, static (type, schemas) => schemas.SchemaFor(type)?.Schema, _eventTypeSchemas);
}
