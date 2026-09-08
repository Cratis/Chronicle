// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Extension methods for converting between <see cref="Contracts.Observation.ObserverFilters"/> and <see cref="ObserverFilters"/>.
/// </summary>
internal static class ObserverFiltersConverters
{
    /// <summary>
    /// Convert from <see cref="Contracts.Observation.ObserverFilters"/> to <see cref="ObserverFilters"/>.
    /// </summary>
    /// <param name="filters"><see cref="Contracts.Observation.ObserverFilters"/> to convert from.</param>
    /// <returns>Converted <see cref="ObserverFilters"/>.</returns>
    public static ObserverFilters ToChronicle(this Contracts.Observation.ObserverFilters filters) =>
        new(
            filters.FilterTags,
            string.IsNullOrEmpty(filters.EventSourceType) ? EventSourceType.Unspecified : new EventSourceType(filters.EventSourceType),
            new EventStreamType(filters.EventStreamType));
}
