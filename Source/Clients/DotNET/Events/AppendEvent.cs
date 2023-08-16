// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Auditing;

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventSourceId"><see cref="EventSourceId"/> for the event to append to.</param>
/// <param name="EventType">The <see cref="EventType"/> to append.</param>
/// <param name="Content">The content to of the event append.</param>
/// <param name="Causation">Causation associated with appending.</param>
/// <param name="ValidFrom">Optional valid from.</param>
public record AppendEvent(
    EventSourceId EventSourceId,
    EventType EventType,
    JsonObject Content,
    IEnumerable<Causation> Causation,
    DateTimeOffset? ValidFrom);
