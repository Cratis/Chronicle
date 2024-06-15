// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Command for redacting events.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to redact.</param>
/// <param name="Reason">The reason for redaction.</param>
/// <param name="EventTypes">Any specific event types to redact. Can be empty.</param>
/// <param name="Causation">Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy"><see cref="CausedBy"/> to identify the person, system or service that caused the event.</param>
public record RedactEvents(
    EventSourceId EventSourceId,
    RedactionReason Reason,
    IEnumerable<EventTypeId> EventTypes,
    IEnumerable<Causation> Causation,
    Identity CausedBy);
