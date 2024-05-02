// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.API.Auditing;
using Cratis.API.Identities;

namespace Cratis.API.EventSequences.Commands;

/// <summary>
/// Represents the payload for appending many events.
/// </summary>
/// <param name="EventSourceId">The event source id to append to.</param>
/// <param name="Events">The events to append.</param>
/// <param name="Causation">Optional Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy">Optional <see cref="CausedBy"/> to identify the person, system or service that caused the events.</param>
public record AppendManyEvents(
    string EventSourceId,
    IEnumerable<EventToAppend> Events,
    IEnumerable<Causation>? Causation,
    Identity? CausedBy);
