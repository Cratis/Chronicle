// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Attribute used to restrict an observer so that it only handles events that carry a specific tag.
/// </summary>
/// <remarks>
/// Apply this attribute to a reactor or reducer class to filter the observed event stream
/// to events that have been tagged with the given value. Use <see cref="TagAttribute"/> or
/// <see cref="TagsAttribute"/> when you want to <em>label</em> an observer; use this attribute when
/// you want to <em>filter</em> on an event's tags.
/// <para>
/// A projection is not one of them. It observes every event of the types its definition declares, and cannot
/// filter on event metadata at all - there is no field for a filter anywhere in a projection's definition, so
/// no client can express one and no kernel could honour it. Narrow a projection by the event types it declares,
/// or pair it with a reactor or reducer that owns the filtered subset. See Documentation/projections/filtering.
/// </para>
/// </remarks>
/// <param name="tag">The tag value that an event must carry in order to be dispatched to the observer.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class FilterEventsByTagAttribute(string tag) : Attribute
{
    /// <summary>
    /// Gets the tag to filter by.
    /// </summary>
    public string Tag { get; } = tag;
}
