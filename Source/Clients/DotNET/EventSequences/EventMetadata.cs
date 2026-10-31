// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents event metadata, without payload, generation, revisions or compliance release.
/// </summary>
/// <param name="SequenceNumber">The locator within the event sequence.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="EventSourceType">The source type.</param>
/// <param name="EventSourceId">The source identifier.</param>
/// <param name="EventStreamType">The stream type.</param>
/// <param name="EventStreamId">The stream identifier.</param>
/// <param name="Occurred">When the event occurred.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="Causation">The causation chain.</param>
/// <param name="CausedBy">The current caused-by identity chain.</param>
/// <param name="InitiatorType">The kind of initiator.</param>
/// <param name="Tags">The event tags.</param>
/// <param name="Subject">The compliance subject.</param>
/// <param name="EventSourceName">The registered source name.</param>
public record EventMetadata(
    EventSequenceNumber SequenceNumber,
    EventTypeId EventTypeId,
    EventSourceType EventSourceType,
    EventSourceId EventSourceId,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    DateTimeOffset Occurred,
    CorrelationId CorrelationId,
    IImmutableList<Causation> Causation,
    ResolvedIdentity CausedBy,
    InitiatorType InitiatorType,
    IImmutableList<Tag> Tags,
    Subject Subject,
    EventSourceName EventSourceName)
{
    /// <summary>
    /// The maximum number of requested locators, before deduplication.
    /// </summary>
    public const int MaxLocators = 500;
}
