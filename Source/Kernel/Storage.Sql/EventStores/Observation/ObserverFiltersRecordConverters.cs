// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observation;

/// <summary>
/// Provides extension methods for converting between <see cref="ObserverFilters"/> and <see cref="ObserverFiltersRecord"/>.
/// </summary>
public static class ObserverFiltersRecordConverters
{
    /// <summary>
    /// Converts an <see cref="ObserverFilters"/> to an <see cref="ObserverFiltersRecord"/>.
    /// </summary>
    /// <param name="filters">The <see cref="ObserverFilters"/> to convert.</param>
    /// <returns>The converted <see cref="ObserverFiltersRecord"/>.</returns>
    public static ObserverFiltersRecord ToRecord(this ObserverFilters filters) =>
        new()
        {
            FilterTags = filters.Tags.ToArray(),
            EventSourceType = filters.EventSourceType?.Value ?? string.Empty,
            EventStreamType = filters.EventStreamType?.Value ?? EventStreamType.All.Value
        };

    /// <summary>
    /// Converts an <see cref="ObserverFiltersRecord"/> to an <see cref="ObserverFilters"/>.
    /// </summary>
    /// <param name="record">The <see cref="ObserverFiltersRecord"/> to convert.</param>
    /// <returns>The converted <see cref="ObserverFilters"/>.</returns>
    public static ObserverFilters ToKernel(this ObserverFiltersRecord record) =>
        new(
            record.FilterTags,
            string.IsNullOrEmpty(record.EventSourceType) ? EventSourceType.Unspecified : new EventSourceType(record.EventSourceType),
            new EventStreamType(record.EventStreamType));
}
