// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the filters to apply when an observer subscribes to an event sequence.
/// </summary>
[ProtoContract]
public class ObserverFilters
{
    /// <summary>
    /// Gets or sets the tags used to filter which events are observed.
    /// Only events tagged with at least one of these tags are dispatched to the observer.
    /// An empty collection means no tag filtering is applied.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<string> FilterTags { get; set; } = [];

    /// <summary>
    /// Gets or sets the event source type filter. An empty string means no filter (all event source types).
    /// </summary>
    [ProtoMember(2)]
    public string EventSourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event stream type filter. Defaults to "All" which means no filter.
    /// </summary>
    [ProtoMember(3), DefaultValue("All")]
    public string EventStreamType { get; set; } = "All";
}
