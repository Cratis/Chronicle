// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Api.Auditing;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Api.Identities;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventSourceType">The event source type.</param>
/// <param name="EventStreamType">The event stream type.</param>
/// <param name="EventStreamId">The event stream identifier.</param>
/// <param name="EventType">The <see cref="EventType"/> to append.</param>
/// <param name="Content">The content of the event represented as <see cref="JsonObject"/>.</param>
/// <param name="Causation">Optional Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy">Optional <see cref="CausedBy"/> to identify the person, system or service that caused the event.</param>
public record AppendEvent(
    string EventSourceId,
    string EventSourceType,
    string EventStreamType,
    string EventStreamId,
    EventType EventType,
    JsonObject Content,
    IEnumerable<Causation>? Causation,
    Identity? CausedBy);
