// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to append for.</param>
/// <param name="EventType">The <see cref="EventType">type of event</see> to append.</param>
/// <param name="Content">The JSON payload of the event.</param>
/// <param name="ValidFrom">Optional date and time for when the compensation is valid from. </param>
public record EventToAppend(EventSourceId EventSourceId, EventType EventType, JsonObject Content, DateTimeOffset? ValidFrom = default);
