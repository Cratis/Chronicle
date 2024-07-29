// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Json;

namespace Cratis.Chronicle.Services.EventSequences;

/// <summary>
/// Extension methods for converting between <see cref="Contracts.Events.EventToAppend"/> and <see cref="EventToAppend"/>.
/// </summary>
public static class EventToAppendConverters
{
    /// <summary>
    /// Convert to a Chronicle representation of <see cref="Contracts.Events.EventToAppend"/>.
    /// </summary>
    /// <param name="eventToAppend"><see cref="Contracts.Events.EventToAppend"/> to convert from.</param>
    /// <returns>A converted <see cref="EventToAppend"/>.</returns>
    public static EventToAppend ToChronicle(this Contracts.Events.EventToAppend eventToAppend) =>
        new(
            eventToAppend.EventType.ToChronicle(),
            JsonSerializer.Deserialize<JsonNode>(eventToAppend.Content, Globals.JsonSerializerOptions)!.AsObject());

    /// <summary>
    /// Convert a collection to a Chronicle representation of <see cref="Contracts.Events.EventToAppend"/>.
    /// </summary>
    /// <param name="eventsToAppend">Collection of <see cref="Contracts.Events.EventToAppend"/> to convert from.</param>
    /// <returns>A converted collection of <see cref="EventToAppend"/>.</returns>
    public static IEnumerable<EventToAppend> ToChronicle(this IEnumerable<Contracts.Events.EventToAppend> eventsToAppend) =>
        eventsToAppend.Select(_ => _.ToChronicle()).ToArray();
}
