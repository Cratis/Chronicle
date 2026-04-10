// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Attribute used to specify the <see cref="Events.EventStreamType"/> that an observer (Reactor, Reducer or Projection) is interested in.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventStreamTypeAttribute"/>.
/// </remarks>
/// <param name="eventStreamType">The event stream type value.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class EventStreamTypeAttribute(string eventStreamType) : Attribute
{
    /// <summary>
    /// Gets the event stream type.
    /// </summary>
    public Events.EventStreamType EventStreamType { get; } = new Events.EventStreamType(eventStreamType);
}
