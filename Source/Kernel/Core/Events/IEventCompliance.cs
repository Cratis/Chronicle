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
    /// Decrypts PII fields for a collection of events, skipping events whose type is not in the schema map,
    /// whose subject is null, or whose schema has no compliance metadata.
    /// </summary>
    /// <param name="events">The events to decrypt.</param>
    /// <param name="eventTypeSchemas">Lookup of <see cref="EventTypeSchema"/> keyed by <see cref="EventType"/>.</param>
    /// <returns>Array of events with PII content decrypted where applicable.</returns>
    Task<AppendedEvent[]> Release(IEnumerable<AppendedEvent> events, IDictionary<EventType, EventTypeSchema> eventTypeSchemas);

    /// <summary>
    /// Releases current event content and reports confirmed erased paths in that content only.
    /// </summary>
    /// <param name="event">The event to release.</param>
    /// <param name="schema">The content schema.</param>
    /// <returns>The event with released content and erased paths, not an erasure audit.</returns>
    Task<AppendedEvent> ReleaseReportingErasure(AppendedEvent @event, JsonSchema schema) => Release(@event, schema);

    /// <summary>
    /// Releases current content and reports erased paths, skipping missing schemas and subjects.
    /// </summary>
    /// <param name="events">The events to release.</param>
    /// <param name="eventTypeSchemas">The schemas keyed by event type.</param>
    /// <returns>The events with released content and confirmed erased paths.</returns>
    Task<AppendedEvent[]> ReleaseReportingErasure(IEnumerable<AppendedEvent> events, IDictionary<EventType, EventTypeSchema> eventTypeSchemas) => Release(events, eventTypeSchemas);
}
