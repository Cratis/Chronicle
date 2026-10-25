// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Defines a service for applying compliance release to event content.
/// </summary>
public interface IEventCompliance
{
    /// <summary>
    /// Decrypts PII fields in an appended event content payload for a given schema.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/> to decrypt.</param>
    /// <param name="schema">The <see cref="JsonSchema"/> describing the event payload.</param>
    /// <returns>The event with decrypted content.</returns>
    Task<AppendedEvent> Release(AppendedEvent @event, JsonSchema schema);

    /// <summary>
    /// Releases generational, original and revision content using each payload's generation schema and the event subject,
    /// without releasing the current content. Erased protected values become empty.
    /// </summary>
    /// <param name="event">The event whose history to release.</param>
    /// <param name="eventTypeSchemas">Lookup of generation-specific schemas. Payloads without a schema or compliance metadata are unchanged.</param>
    /// <returns>The event with released history, or the unchanged event when its subject is null.</returns>
    Task<AppendedEvent> ReleaseHistory(AppendedEvent @event, IDictionary<EventType, EventTypeSchema> eventTypeSchemas);

    /// <summary>
    /// Releases current content and history for a collection of events, using each payload's generation schema.
    /// Payloads without a schema or compliance metadata are unchanged. Events whose subject is null are unchanged.
    /// </summary>
    /// <param name="events">The events to decrypt.</param>
    /// <param name="eventTypeSchemas">Lookup of <see cref="EventTypeSchema"/> keyed by <see cref="EventType"/>.</param>
    /// <returns>Array of events with PII content decrypted where applicable.</returns>
    Task<AppendedEvent[]> Release(IEnumerable<AppendedEvent> events, IDictionary<EventType, EventTypeSchema> eventTypeSchemas);
}
