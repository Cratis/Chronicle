// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the filters to apply when an observer subscribes to an event sequence.
/// </summary>
/// <param name="Tags">Collection of tags used to filter which events are observed. Only events tagged with at least one of these tags are dispatched to the observer.</param>
/// <param name="EventSourceType">Optional <see cref="Events.EventSourceType"/> filter. When <see cref="Events.EventSourceType.Unspecified"/>, all event source types are observed.</param>
/// <param name="EventStreamType">Optional <see cref="Events.EventStreamType"/> filter. When <see cref="Events.EventStreamType.All"/>, all event stream types are observed.</param>
public record ObserverFilters(
    IEnumerable<string> Tags,
    EventSourceType? EventSourceType = default,
    EventStreamType? EventStreamType = default)
{
    /// <summary>
    /// Gets a default <see cref="ObserverFilters"/> that applies no filtering.
    /// </summary>
    public static readonly ObserverFilters None = new([]);
}
