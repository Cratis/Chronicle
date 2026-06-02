// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a reactor that can dynamically provide the <see cref="EventStreamId"/> to use when
/// appending side-effect events returned from handler methods.
/// </summary>
/// <remarks>
/// Implement this interface on a reactor class to supply a runtime stream ID. For a fixed compile-time
/// value, apply the <see cref="Events.EventStreamIdAttribute"/> to the reactor type instead.
/// When both this interface and the attribute are present on the same reactor type, the interface
/// takes priority and the attribute value is ignored.
/// </remarks>
public interface ICanProvideEventStreamId
{
    /// <summary>
    /// Gets the <see cref="EventStreamId"/> to use when appending side-effect events.
    /// </summary>
    /// <returns>The <see cref="EventStreamId"/>.</returns>
    EventStreamId GetEventStreamId();
}
