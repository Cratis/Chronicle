// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Defines a system that is capable of converting between <see cref="Event"/> and <see cref="AppendedEvent"/>.
/// </summary>
public interface IEventConverter
{
    /// <summary>
    /// Convert a <see cref="Event"/> to <see cref="AppendedEvent"/>.
    /// </summary>
    /// <param name="event"><see cref="Event"/> to convert from.</param>
    /// <returns>Converted <see cref="AppendedEvent"/>.</returns>
    Task<AppendedEvent> ToAppendedEvent(Event @event);
}
