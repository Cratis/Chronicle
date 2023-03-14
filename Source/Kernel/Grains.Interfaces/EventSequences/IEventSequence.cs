// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Defines the event sequence.
/// </summary>
public interface IEventSequence : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Get the next sequence number.
    /// </summary>
    /// <returns>Next sequence number.</returns>
    Task<EventSequenceNumber> GetNextSequenceNumber();

    /// <summary>
    /// Get the sequence number of the last (tail) event in the sequence.
    /// </summary>
    /// <returns>Tail sequence number.</returns>
    Task<EventSequenceNumber> GetTailSequenceNumber();

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to append.</param>
    /// <param name="content">The JSON payload of the event.</param>
    /// <param name="validFrom">Optional date and time for when the compensation is valid from. </param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Append(EventSourceId eventSourceId, EventType eventType, JsonObject content, DateTimeOffset? validFrom = default);

    /// <summary>
    /// Compensate a specific event in the event store.
    /// </summary>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the event to compensate.</param>
    /// <param name="eventType">The <see cref="EventType">type of event</see> to compensate.</param>
    /// <param name="content">The JSON payload of the event.</param>
    /// <param name="validFrom">Optional date and time for when the compensation is valid from. </param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    /// <remarks>
    /// The type of the event has to be the same as the original event at the sequence number.
    /// Its generational information is taken into account when compensating.
    /// </remarks>
    Task Compensate(EventSequenceNumber sequenceNumber, EventType eventType, string content, DateTimeOffset? validFrom = default);

    /// <summary>
    /// Redact an event at a specific sequence number.
    /// </summary>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to redact.</param>
        /// <param name="reason">Reason for redacting.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason);

    /// <summary>
    /// Redact all events for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to redact.</param>
    /// <param name="reason">Reason for redacting.</param>
    /// <param name="eventTypes">Optionally any specific event types.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Redact(EventSourceId eventSourceId, RedactionReason reason, IEnumerable<EventType> eventTypes);
}
