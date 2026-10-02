// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle;

/// <summary>
/// Extension methods for working with tags.
/// </summary>
public static class TagExtensions
{
    /// <summary>
    /// Get all tags from a type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>Collection of tag values.</returns>
    public static IEnumerable<string> GetTags(this Type type)
    {
        var tagAttributes = type.GetCustomAttributes<TagAttribute>();
        var tagsAttributes = type.GetCustomAttributes<TagsAttribute>();

        return tagAttributes.SelectMany(_ => _.Tags)
            .Concat(tagsAttributes.SelectMany(_ => _.Tags))
            .Distinct();
    }

    /// <summary>
    /// Get all filter tags from a type.
    /// </summary>
    /// <remarks>
    /// Filter tags are specified via <see cref="FilterEventsByTagAttribute"/> and control which events the observer receives,
    /// as opposed to <see cref="TagAttribute"/> and <see cref="TagsAttribute"/> which label the observer itself.
    /// </remarks>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>Collection of tag values to filter events by.</returns>
    public static IEnumerable<string> GetFilterTags(this Type type) =>
        type.GetCustomAttributes<FilterEventsByTagAttribute>().Select(_ => _.Tag).Distinct();

    /// <summary>
    /// Get the <see cref="EventSourceType"/> filter from a type, if one is specified.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="EventSourceType"/> if an <see cref="EventSourceTypeAttribute"/> is present; otherwise <see cref="EventSourceType.Unspecified"/>.</returns>
    public static EventSourceType GetEventSourceType(this Type type)
    {
        var attribute = type.GetCustomAttribute<EventSourceTypeAttribute>();
        return attribute?.EventSourceType ?? EventSourceType.Unspecified;
    }

    /// <summary>
    /// Gets the event source and stream filters from a type, resolving typed filters against registered definitions.
    /// </summary>
    /// <param name="type">The observer type to inspect.</param>
    /// <param name="eventSources">The registered event source definitions.</param>
    /// <returns>The event source and event stream filters.</returns>
    /// <exception cref="UnknownEventSource">Thrown when the typed filter's event source is not registered.</exception>
    /// <exception cref="EventStreamDoesNotBelongToEventSource">Thrown when the typed filter's stream is not declared by its event source.</exception>
    public static (EventSourceType EventSourceType, EventStreamType EventStreamType) GetEventSourceFilter(this Type type, IEventSources eventSources)
    {
        var attribute = type.GetCustomAttributes()
            .FirstOrDefault(attribute => attribute.GetType().IsGenericType && attribute.GetType().GetGenericTypeDefinition() == typeof(FromEventSourceAttribute<>));
        if (attribute is null)
        {
            return (type.GetEventSourceType(), type.GetEventStreamType());
        }

        var eventSourceType = attribute.GetType().GetGenericArguments()[0];
        var stream = (string)attribute.GetType().GetProperty(nameof(FromEventSourceAttribute<IEventSource>.Stream))!.GetValue(attribute)!;
        var definition = eventSources.GetFor(eventSourceType);
        var eventStream = definition.FindStream(stream) ?? throw new EventStreamDoesNotBelongToEventSource(definition.Name, stream);

        return (definition.EventSourceType, eventStream.EventStreamType);
    }

    /// <summary>
    /// Get the <see cref="EventStreamType"/> filter from a type, if one is specified.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="EventStreamType"/> if an <see cref="EventStreamTypeAttribute"/> is present; otherwise <see cref="EventStreamType.All"/>.</returns>
    public static EventStreamType GetEventStreamType(this Type type)
    {
        var attribute = type.GetCustomAttribute<EventStreamTypeAttribute>();
        return attribute?.EventStreamType ?? EventStreamType.All;
    }
}
