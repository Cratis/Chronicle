// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Api.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceCommands"/> class.
/// </remarks>
[Route("/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}")]
public class EventSequenceCommands : ControllerBase
{
    /// <summary>
    /// Appends an event to the event log.
    /// </summary>
    /// <param name="eventStore">The event store to append for.</param>
    /// <param name="namespace">The namespace to append to.</param>
    /// <param name="eventSequenceId">The event sequence to append to.</param>
    /// <param name="eventToAppend">The payload with the details about the event to append.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public async Task Append(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromBody] AppendEvent eventToAppend)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Appends an event to the event log.
    /// </summary>
    /// <param name="eventStore">The event store to append for.</param>
    /// <param name="namespace">The namespace to append to.</param>
    /// <param name="eventSequenceId">The event sequence to append to.</param>
    /// <param name="eventsToAppend">The payload with the details about the events to append.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("append-many")]
    public async Task AppendMany(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromBody] AppendManyEvents eventsToAppend)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Redact a specific single event by its sequence number.
    /// </summary>
    /// <param name="eventStore">The event store to append for.</param>
    /// <param name="namespace">The namespace to append to.</param>
    /// <param name="eventSequenceId">The event sequence to redact for.</param>
    /// <param name="redaction">The <see cref="Redact"/> to redact.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("redact-event")]
    public async Task Redact(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromBody] RedactEvent redaction)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Redact multiple events.
    /// </summary>
    /// <param name="eventStore">The event store to append for.</param>
    /// <param name="namespace">The namespace to append to.</param>
    /// <param name="eventSequenceId">The event sequence to redact for.</param>
    /// <param name="redaction">The redaction filter to use.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("redact-events")]
    public async Task RedactMany(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromBody] RedactEvents redaction)
    {
        throw new NotImplementedException();
    }
}
