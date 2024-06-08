// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

#pragma warning disable SA1600, IDE0060

namespace Aksio.Cratis.Kernel.Domain.EventSequences;

/// <summary>
/// Represents the payload for appending an event.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/>.</param>
/// <param name="EventType">The <see cref="EventType"/> to append.</param>
/// <param name="Content">The content of the event represented as <see cref="JsonObject"/>.</param>
/// <param name="Causation">Optional Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy">Optional <see cref="CausedBy"/> to identify the person, system or service that caused the event.</param>
/// <param name="ValidFrom">Optional valid from.</param>
public record AppendEvent(
    EventSourceId EventSourceId,
    EventType EventType,
    JsonObject Content,
    IEnumerable<Causation>? Causation,
    Identity? CausedBy,
    DateTimeOffset? ValidFrom);
