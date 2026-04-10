// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

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
    /// Filter tags are specified via <see cref="FilterByTagAttribute"/> and control which events the observer receives,
    /// as opposed to <see cref="TagAttribute"/> and <see cref="TagsAttribute"/> which label the observer itself.
    /// </remarks>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>Collection of tag values to filter events by.</returns>
    public static IEnumerable<string> GetFilterTags(this Type type) =>
        type.GetCustomAttributes<FilterByTagAttribute>().Select(_ => _.Tag).Distinct();

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
