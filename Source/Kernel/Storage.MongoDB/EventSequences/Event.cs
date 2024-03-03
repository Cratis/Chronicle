// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Auditing;
using Cratis.Events;
using Cratis.Identities;
using MongoDB.Bson;

namespace Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Represents the document representation of a stored event.
/// </summary>
/// <param name="SequenceNumber">The sequence number of the event - the primary key.</param>
/// <param name="CorrelationId">The unique identifier used to correlation.</param>
/// <param name="Causation">Chain of causation for the event.</param>
/// <param name="CausedBy">Chain of person, system or service that caused the event.</param>
/// <param name="Type">The <see cref="EventTypeId">type identifier</see> of the event.</param>
/// <param name="Occurred">The time the event occurred.</param>
/// <param name="ValidFrom">The date and time the event is considered valid from.</param>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> for the event.</param>
/// <param name="Content">The content per event type generation.</param>
/// <param name="Compensations">Any compensations for the event.</param>
public record Event(
    EventSequenceNumber SequenceNumber,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<IdentityId> CausedBy,
    EventTypeId Type,
    DateTimeOffset Occurred,
    DateTimeOffset ValidFrom,
    EventSourceId EventSourceId,
    IDictionary<string, BsonDocument> Content,
    IEnumerable<EventCompensation> Compensations);
