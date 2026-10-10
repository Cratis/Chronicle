// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents a snapshot of an event's base generations, independent of revision delivery.
/// </summary>
/// <param name="SequenceNumber">The event's sequence number.</param>
/// <param name="EventTypeId">The stored event type, including redaction markers.</param>
/// <param name="EventSourceId">The event source.</param>
/// <param name="Subject">The compliance subject.</param>
/// <param name="AppendedGeneration">The known appended generation, or null for legacy events.</param>
/// <param name="Content">The protected base JSON per generation, excluding separate revisions.</param>
/// <param name="RevisionCount">The observed revision count, zero for providers revising in place.</param>
/// <param name="ConcurrencyToken">The provider-defined concurrency token.</param>
public record StoredEventGenerations(
    EventSequenceNumber SequenceNumber,
    EventTypeId EventTypeId,
    EventSourceId EventSourceId,
    Subject Subject,
    EventTypeGeneration? AppendedGeneration,
    IReadOnlyDictionary<EventTypeGeneration, string> Content,
    int RevisionCount,
    string ConcurrencyToken);
