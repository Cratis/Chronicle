// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the scope of concurrency for an event sequence operation.
/// </summary>
/// <param name="SequenceNumber">The expected sequence number for the operation.</param>
/// <param name="EventSourceId">The value indicating whether to scope to the event source id.</param>
/// <param name="EventStreamType">Optional event stream type to scope to. If not set, it will not be used.</param>
/// <param name="EventStreamId">Optional event stream identifier to scope to. If not set, it will not be used.</param>
/// <param name="EventSourceType">Optional event source type to scope to. If not set, it will not be used.</param>
/// <param name="EventTypes">Optional collection of event types to scope to. If not set, it will not be used.</param>
/// <param name="ExpectsNoMatchingEvent">Whether the scope expects no event matching its narrowing to exist yet.</param>
/// <remarks>
/// Deliberately its own type rather than <see cref="Concepts.EventSequences.Concurrency.ConcurrencyScope"/> directly
/// - the same reason <see cref="EventType"/> is its own local type rather than <see cref="Concepts.Events.EventType"/>
/// directly: it keeps this command's wire shape mirroring into this service's own <c>Contracts.Sequences</c>
/// namespace, rather than reaching into <c>Contracts.EventSequences.Concurrency</c>, where a hand-written contract
/// of the same name still serves the not-yet-retired <c>EventSequences</c> service.
/// </remarks>
public record ConcurrencyScope(
    ulong SequenceNumber,
    bool EventSourceId,
    string? EventStreamType = default,
    string? EventStreamId = default,
    string? EventSourceType = default,
    IEnumerable<EventType>? EventTypes = default,
    bool ExpectsNoMatchingEvent = default);
