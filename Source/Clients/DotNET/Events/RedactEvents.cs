// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Command for redacting events.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to redact.</param>
/// <param name="Reason">The reason for redaction.</param>
/// <param name="EventTypes">Any specific event types to redact. Can be empty.</param>
public record RedactEvents(EventSourceId EventSourceId, RedactionReason Reason, IEnumerable<EventTypeId> EventTypes);
