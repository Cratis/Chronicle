// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Api.Auditing;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Api.Identities;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Api.EventSequences;

/// <summary>
/// Represents the API for working with the event log.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceCommands"/> class.
/// </remarks>
/// <param name="eventSequences"><see cref="IEventSequences"/> service for working with event sequences.</param>
[Route("/api/event-store/{eventStore}/{namespace}/sequence/{eventSequenceId}")]
public class EventSequenceCommands(IEventSequences eventSequences) : ControllerBase
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
    /// Compensate a specific single event by its sequence number.
    /// </summary>
    /// <param name="eventStore">The event store to append for.</param>
    /// <param name="namespace">The namespace to append to.</param>
    /// <param name="eventSequenceId">The event sequence to compensate for.</param>
    /// <param name="compensation">The <see cref="CompensateEvent"/> to compensate.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("compensate-event")]
    public async Task Compensate(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string eventSequenceId,
        [FromBody] CompensateEvent compensation)
    {
        await eventSequences.Compensate(new CompensateRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            SequenceNumber = compensation.SequenceNumber,
            EventType = compensation.EventType.ToContract(),
            Content = JsonSerializer.Serialize(compensation.Content),
            CorrelationId = Guid.NewGuid(),
            Causation = compensation.Causation?.ToContract().ToList() ?? [],
            CausedBy = compensation.CausedBy?.ToContract() ?? new()
        });
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
