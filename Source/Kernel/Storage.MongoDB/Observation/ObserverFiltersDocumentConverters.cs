// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Provides extension methods for converting between <see cref="ObserverFilters"/> and <see cref="ObserverFiltersDocument"/>.
/// </summary>
public static class ObserverFiltersDocumentConverters
{
    /// <summary>
    /// Converts an <see cref="ObserverFilters"/> to an <see cref="ObserverFiltersDocument"/>.
    /// </summary>
    /// <param name="filters">The <see cref="ObserverFilters"/> to convert.</param>
    /// <returns>The converted <see cref="ObserverFiltersDocument"/>.</returns>
    public static ObserverFiltersDocument ToDocument(this ObserverFilters filters) =>
        new()
        {
            FilterTags = filters.Tags.ToArray(),
            EventSourceType = filters.EventSourceType?.Value ?? string.Empty,
            EventStreamType = filters.EventStreamType?.Value ?? EventStreamType.All.Value
        };

    /// <summary>
    /// Converts an <see cref="ObserverFiltersDocument"/> to an <see cref="ObserverFilters"/>.
    /// </summary>
    /// <param name="document">The <see cref="ObserverFiltersDocument"/> to convert.</param>
    /// <returns>The converted <see cref="ObserverFilters"/>.</returns>
    public static ObserverFilters ToKernel(this ObserverFiltersDocument document) =>
        new(
            document.FilterTags,
            string.IsNullOrEmpty(document.EventSourceType) ? EventSourceType.Unspecified : new EventSourceType(document.EventSourceType),
            new EventStreamType(document.EventStreamType));
}
