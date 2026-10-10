// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents event metadata without content, revisions or compliance release.
/// </summary>
/// <param name="SequenceNumber">The sequence locator.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="EventSourceType">The source type.</param>
/// <param name="EventSourceId">The source identifier.</param>
/// <param name="EventStreamType">The stream type.</param>
/// <param name="EventStreamId">The stream identifier.</param>
/// <param name="Occurred">When the event occurred.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="Causation">The causation chain.</param>
/// <param name="CausedBy">The identity chain resolved at read time.</param>
/// <param name="InitiatorType">The kind of initiator at the chain head.</param>
/// <param name="Tags">The event tags.</param>
/// <param name="Subject">The compliance subject.</param>
/// <param name="EventSourceName">The registered source name.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record EventMetadata(
    EventSequenceNumber SequenceNumber,
    EventTypeId EventTypeId,
    EventSourceType EventSourceType,
    EventSourceId EventSourceId,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    DateTimeOffset Occurred,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    ResolvedIdentity CausedBy,
    InitiatorType InitiatorType,
    IEnumerable<Tag> Tags,
    Subject Subject,
    EventSourceName EventSourceName)
{
    /// <summary>
    /// Reads metadata at up to 500 locators. Missing locators are omitted.
    /// </summary>
    /// <param name="storage">The event and identity storage.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The sequence.</param>
    /// <param name="sequenceNumbers">The sequence locators.</param>
    /// <returns>The matching metadata with current identity names.</returns>
    internal static Task<IEnumerable<EventMetadata>> MetadataAt(
        IStorage storage,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        IEnumerable<ulong> sequenceNumbers) => EventMetadataQuerying.Read(storage, eventStore, @namespace, eventSequenceId, sequenceNumbers);
}
