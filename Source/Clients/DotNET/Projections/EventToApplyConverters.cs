// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;

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
    /// <param name="jsonSerializerOptions">JSON serializer options to use.</param>
    /// <returns>Converted <see cref="Contracts.Projections.EventToApply"/>.</returns>
    internal static Contracts.Projections.EventToApply ToContract(this EventToApply eventToApply, JsonSerializerOptions jsonSerializerOptions) =>
        new()
        {
            EventType = eventToApply.EventType.ToContract(),
            Content = JsonSerializer.Serialize(eventToApply.Content, jsonSerializerOptions)
        };

    /// <summary>
    /// Convert to a collection to contract representation.
    /// </summary>
    /// <param name="eventsToApply">Collection <see cref="EventToApply"/> to convert from.</param>
    /// <param name="jsonSerializerOptions">JSON serializer options to use.</param>
    /// <returns>Converted collection of <see cref="Contracts.Projections.EventToApply"/>.</returns>
    internal static IList<Contracts.Projections.EventToApply> ToContract(this IEnumerable<EventToApply> eventsToApply, JsonSerializerOptions jsonSerializerOptions) =>
        eventsToApply.Select(_ => _.ToContract(jsonSerializerOptions)).ToList();
}
