// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents an event to append to storage.
/// </summary>
/// <param name="SequenceNumber">The unique <see cref="EventSequenceNumber">sequence number</see> within the event sequence.</param>
/// <param name="EventSourceType">The <see cref="EventSourceType">event source</see> to append for.</param>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to append for.</param>
/// <param name="EventStreamType">The <see cref="EventStreamType"/> to append to.</param>
/// <param name="EventStreamId">The <see cref="EventStreamId"/> to append to.</param>
/// <param name="EventType">The <see cref="EventType">type of event</see> to append.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> for the event.</param>
/// <param name="Causation">Collection of <see cref="Causation"/>.</param>
/// <param name="CausedByChain">The chain of <see cref="IdentityId"/> representing the person, system or service that caused the event.</param>
/// <param name="Tags">Collection of tags associated with the event.</param>
/// <param name="Occurred">The date and time the event occurred.</param>
/// <param name="Content">The content of the event.</param>
/// <param name="Hash">The <see cref="EventHash"/> of the event content.</param>
public record EventToAppendToStorage(
    EventSequenceNumber SequenceNumber,
    EventSourceType EventSourceType,
    EventSourceId EventSourceId,
    EventStreamType EventStreamType,
    EventStreamId EventStreamId,
    EventType EventType,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<IdentityId> CausedByChain,
    IEnumerable<Tag> Tags,
    DateTimeOffset Occurred,
    ExpandoObject Content,
    EventHash Hash);
