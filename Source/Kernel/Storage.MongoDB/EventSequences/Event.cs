// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents the document representation of a stored event.
/// </summary>
/// <param name="SequenceNumber">The sequence number of the event - the primary key.</param>
/// <param name="CorrelationId">The unique identifier used to correlation.</param>
/// <param name="Causation">Chain of causation for the event.</param>
/// <param name="CausedBy">Chain of person, system or service that caused the event.</param>
/// <param name="Type">The <see cref="EventTypeId">type identifier</see> of the event.</param>
/// <param name="Occurred">The time the event occurred.</param>
/// <param name="EventSource">The <see cref="EventSource"/> for the event.</param>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> for the event.</param>
/// <param name="EventStreamType">the <see cref="EventStreamType"/> to append to.</param>
/// <param name="EventStreamId">The <see cref="EventStreamId"/> to append to.</param>
/// <param name="Content">The content per event type generation.</param>
/// <param name="Compensations">Any compensations for the event.</param>
public record Event(
    EventSequenceNumber SequenceNumber,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<IdentityId> CausedBy,
    EventTypeId Type,
    DateTimeOffset Occurred,
    EventSource EventSource,
    EventSourceId EventSourceId,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    IDictionary<string, BsonDocument> Content,
    IEnumerable<EventCompensation> Compensations);
