// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Api.Auditing;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Api.Identities;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Command for revising single event.
/// </summary>
/// <param name="SequenceNumber">The event sequence number to revise.</param>
/// <param name="EventType">The type of event to revise with.</param>
/// <param name="Content">The revising event content.</param>
/// <param name="Causation">Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy"><see cref="CausedBy"/> to identify the person, system or service that caused the revision.</param>
public record ReviseEvent(
    ulong SequenceNumber,
    EventType EventType,
    JsonObject Content,
    IEnumerable<Causation>? Causation,
    Identity? CausedBy);
