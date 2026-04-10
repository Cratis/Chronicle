// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Attribute used to specify the <see cref="Events.EventSourceType"/> that an observer (Reactor, Reducer or Projection) is interested in.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventSourceTypeAttribute"/>.
/// </remarks>
/// <param name="eventSourceType">The event source type value.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class EventSourceTypeAttribute(string eventSourceType) : Attribute
{
    /// <summary>
    /// Gets the event source type.
    /// </summary>
    public Events.EventSourceType EventSourceType { get; } = new Events.EventSourceType(eventSourceType);
}
