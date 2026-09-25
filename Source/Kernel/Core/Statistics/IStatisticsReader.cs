// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Statistics;

/// <summary>
/// Defines a system that reads the materialized event statistics.
/// </summary>
public interface IStatisticsReader
{
    /// <summary>
    /// Gets the per event type rows for one namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to read for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to read for.</param>
    /// <returns>The rows.</returns>
    Task<IEnumerable<EventTypeStatistics>> GetEventTypeStatistics(EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Gets the per event type rows for an entire event store, across every namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to read for.</param>
    /// <returns>The rows.</returns>
    Task<IEnumerable<EventTypeStatistics>> GetEventTypeStatisticsForEventStore(EventStoreName eventStore);
}
