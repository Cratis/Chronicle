// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observation;

/// <summary>
/// Represents the SQL storage model for observer filters, serialized as JSON.
/// </summary>
public class ObserverFilters
{
    /// <summary>
    /// Gets or sets the tags used to filter which events are observed.
    /// An empty collection means no tag filtering is applied.
    /// </summary>
    public IEnumerable<string> FilterTags { get; set; } = [];

    /// <summary>
    /// Gets or sets the event source type filter. An empty string means no filter (all event source types).
    /// </summary>
    public string EventSourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event stream type filter. Defaults to "All" which means no filter.
    /// </summary>
    public string EventStreamType { get; set; } = "All";
}
