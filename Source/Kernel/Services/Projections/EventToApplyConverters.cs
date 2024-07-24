// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Services.Projections;

/// <summary>
/// Extension methods for converting to and from <see cref="Contracts.Projections.EventToApply"/>.
/// </summary>
public static class EventToApplyConverters
{
    /// <summary>
    /// Convert to Chronicle representation.
    /// </summary>
    /// <param name="eventToApply"><see cref="Contracts.Projections.EventToApply"/> to convert from.</param>
    /// <returns>Converted <see cref="EventToApply"/>.</returns>
    public static EventToApply ToChronicle(this Contracts.Projections.EventToApply eventToApply) =>
        new(
            eventToApply.EventType.ToChronicle(),
            (JsonObject)JsonNode.Parse(eventToApply.Content)!);
}
