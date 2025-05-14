// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Json;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Extension methods for converting to and from <see cref="Contracts.Projections.EventToApply"/>.
/// </summary>
internal static class EventToApplyConverters
{
    /// <summary>
    /// Convert to contract representation.
    /// </summary>
    /// <param name="eventToApply"><see cref="EventToApply"/> to convert from.</param>
    /// <returns>Converted <see cref="Contracts.Projections.EventToApply"/>.</returns>
    internal static Contracts.Projections.EventToApply ToContract(this EventToApply eventToApply) =>
        new()
        {
            EventType = eventToApply.EventType.ToContract(),
            Content = JsonSerializer.Serialize(eventToApply.Content, Globals.JsonSerializerOptions)
        };

    /// <summary>
    /// Convert to a collection to contract representation.
    /// </summary>
    /// <param name="eventsToApply">Collection <see cref="EventToApply"/> to convert from.</param>
    /// <returns>Converted collection of <see cref="Contracts.Projections.EventToApply"/>.</returns>
    internal static IList<Contracts.Projections.EventToApply> ToContract(this IEnumerable<EventToApply> eventsToApply) =>
        eventsToApply.Select(_ => _.ToContract()).ToList();
}
