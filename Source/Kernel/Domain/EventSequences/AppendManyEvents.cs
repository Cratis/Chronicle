// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;

#pragma warning disable SA1600, IDE0060

namespace Aksio.Cratis.Kernel.Domain.EventSequences;

/// <summary>
/// Represents the payload for appending many events.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to append to.</param>
/// <param name="Events">The events to append.</param>
/// <param name="Causation">Optional Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy">Optional <see cref="CausedBy"/> to identify the person, system or service that caused the events.</param>
public record AppendManyEvents(
    EventSourceId EventSourceId,
    IEnumerable<EventToAppend> Events,
    IEnumerable<Causation>? Causation,
    Identity? CausedBy);
