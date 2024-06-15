// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the payload for appending many events.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to append to.</param>
/// <param name="Events">The events to append.</param>
/// <param name="Causation">Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy"><see cref="CausedBy"/> to identify the person, system or service that caused the event.</param>
public record AppendManyEvents(
    EventSourceId EventSourceId,
    IEnumerable<EventToAppend> Events,
    IEnumerable<Causation> Causation,
    Identity CausedBy);
