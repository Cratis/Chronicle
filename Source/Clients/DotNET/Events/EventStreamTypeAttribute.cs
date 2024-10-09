// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute to specify the <see cref="EventStreamType"/> for an event stream.
/// </summary>
/// <param name="eventStreamType">The <see cref="EventStreamType"/> for the event stream.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EventStreamTypeAttribute(string eventStreamType) : Attribute
{
    /// <summary>
    /// Gets the <see cref="EventStreamType"/>.
    /// </summary>
    public EventStreamType EventStreamType { get; } = eventStreamType;
}
