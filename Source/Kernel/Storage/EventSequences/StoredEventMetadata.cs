// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents stored event metadata, without content, generation, hashes or revisions.
/// </summary>
/// <param name="SequenceNumber">The event locator.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="EventSourceType">The event source type.</param>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventStreamType">The stream type.</param>
/// <param name="EventStreamId">The stream identifier.</param>
/// <param name="Occurred">When the event occurred.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="Causation">The causation chain.</param>
/// <param name="CausedByChain">The stored identity identifiers, in chain order.</param>
/// <param name="Tags">The event tags.</param>
/// <param name="Subject">The compliance subject.</param>
/// <param name="EventSourceName">The registered event source name.</param>
public record StoredEventMetadata(
    EventSequenceNumber SequenceNumber,
    EventTypeId EventTypeId,
    EventSourceType EventSourceType,
    EventSourceId EventSourceId,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    DateTimeOffset Occurred,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<IdentityId> CausedByChain,
    IEnumerable<Tag> Tags,
    Subject Subject,
    EventSourceName EventSourceName);
