// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Attribute used to restrict an observer so that it only handles events that carry a specific tag.
/// </summary>
/// <remarks>
/// Apply this attribute to a reactor, reducer, or projection class to filter the observed event stream
/// to events that have been tagged with the given value.  Use <see cref="TagAttribute"/> or
/// <see cref="TagsAttribute"/> when you want to <em>label</em> an observer; use this attribute when
/// you want to <em>filter</em> on an event's tags.
/// </remarks>
/// <param name="tag">The tag value that an event must carry in order to be dispatched to the observer.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class FilterByTagAttribute(string tag) : Attribute
{
    /// <summary>
    /// Gets the tag to filter by.
    /// </summary>
    public string Tag { get; } = tag;
}
