// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelObserverFilters = Cratis.Chronicle.Concepts.Observation.ObserverFilters;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observation;

/// <summary>
/// Provides extension methods for converting between <see cref="KernelObserverFilters"/> and <see cref="ObserverFilters"/>.
/// </summary>
public static class ObserverFiltersConverters
{
    /// <summary>
    /// Converts a <see cref="KernelObserverFilters"/> to a SQL <see cref="ObserverFilters"/>.
    /// </summary>
    /// <param name="filters">The <see cref="KernelObserverFilters"/> to convert.</param>
    /// <returns>The converted <see cref="ObserverFilters"/>.</returns>
    public static ObserverFilters ToSql(this KernelObserverFilters filters) =>
        new()
        {
            FilterTags = filters.Tags.ToArray(),
            EventSourceType = filters.EventSourceType?.Value ?? string.Empty,
            EventStreamType = filters.EventStreamType?.Value ?? EventStreamType.All.Value
        };

    /// <summary>
    /// Converts a SQL <see cref="ObserverFilters"/> to a <see cref="KernelObserverFilters"/>.
    /// </summary>
    /// <param name="record">The SQL <see cref="ObserverFilters"/> to convert.</param>
    /// <returns>The converted <see cref="KernelObserverFilters"/>.</returns>
    public static KernelObserverFilters ToKernel(this ObserverFilters record) =>
        new(
            record.FilterTags,
            string.IsNullOrEmpty(record.EventSourceType) ? EventSourceType.Unspecified : new EventSourceType(record.EventSourceType),
            new EventStreamType(record.EventStreamType));
}
