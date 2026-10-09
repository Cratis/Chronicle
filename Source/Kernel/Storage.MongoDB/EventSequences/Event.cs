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
/// <param name="EventSourceType">The <see cref="EventSourceType"/> for the event.</param>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> for the event.</param>
/// <param name="EventStreamType">the <see cref="EventStreamType"/> to append to.</param>
/// <param name="EventStreamId">The <see cref="EventStreamId"/> to append to.</param>
/// <param name="Tags">Collection of tags associated with the event.</param>
/// <param name="Content">The content per event type generation.</param>
/// <param name="ContentHashes">The content hashes per event type generation.</param>
/// <param name="Revisions">Any revisions for the event.</param>
/// <param name="Subject">Optional subject that identifies the compliance target for the event.</param>
public record Event(
    EventSequenceNumber SequenceNumber,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<IdentityId> CausedBy,
    EventTypeId Type,
    DateTimeOffset Occurred,
    EventSourceType EventSourceType,
    EventSourceId EventSourceId,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    IEnumerable<string> Tags,
    IDictionary<string, BsonDocument> Content,
    IDictionary<string, string> ContentHashes,
    IEnumerable<EventRevision> Revisions,
    Subject? Subject = null)
{
    /// <summary>
    /// Gets the structured named tags. Missing in historic documents.
    /// </summary>
    public IEnumerable<NamedTagDocument> NamedTags { get; init; } = [];

    /// <summary>
    /// Gets the name of the registered event source definition the event was appended through.
    /// Missing, or null, in historic documents and in events not appended through a definition.
    /// </summary>
    public EventSourceName? EventSource { get; init; }

    /// <summary>
    /// Gets the opaque publication identity, atomically stored with the event. Absent for ordinary appends.
    /// </summary>
    public string? PublicationId { get; init; }

    /// <summary>
    /// Gets the immutable publication intent fingerprint. Preserved through revision and redaction.
    /// </summary>
    public string? PublicationFingerprint { get; init; }
}
