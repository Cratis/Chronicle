// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Collections.Immutable;
using System.Dynamic;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Execution;
using Cratis.Monads;
using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelAuditing = KernelConcepts::Cratis.Chronicle.Concepts.Auditing;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelIdentities = KernelConcepts::Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a null implementation of <see cref="IEventSequenceStorage"/> used for read model context testing.
/// </summary>
/// <remarks>
/// All operations throw <see cref="NotSupportedException"/>. This stub exists solely to satisfy the
/// dependency on <see cref="IEventSequenceStorage"/> in the projection engine. Projections that use
/// joins may call some of these operations; basic projections without joins will not.
/// </remarks>
internal class NullEventSequenceStorage : IEventSequenceStorage
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NullEventSequenceStorage"/>.
    /// </summary>
    public static readonly NullEventSequenceStorage Instance = new();

    /// <inheritdoc/>
    public Task<EventSequenceState> GetState() => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task SaveState(EventSequenceState state) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<KernelEvents::EventCount> GetCount(KernelEvents::EventSequenceNumber? lastEventSequenceNumber = null, IEnumerable<KernelEvents::EventType>? eventTypes = null) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<Result<KernelAppendedEvent, DuplicateEventSequenceNumber>> Append(
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelEvents::EventSourceType eventSourceType,
        KernelEvents::EventSourceId eventSourceId,
        KernelEvents::EventStreamType eventStreamType,
        KernelEvents::EventStreamId eventStreamId,
        KernelEvents::EventType eventType,
        CorrelationId correlationId,
        IEnumerable<KernelAuditing::Causation> causation,
        IEnumerable<KernelIdentities::IdentityId> causedByChain,
        IEnumerable<KernelEvents::Tag> tags,
        DateTimeOffset occurred,
        IDictionary<KernelEvents::EventTypeGeneration, ExpandoObject> content) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<Result<IEnumerable<KernelAppendedEvent>, DuplicateEventSequenceNumber>> AppendMany(IEnumerable<EventToAppendToStorage> events) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task Revise(
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelEvents::EventType eventType,
        CorrelationId correlationId,
        IEnumerable<KernelAuditing::Causation> causation,
        IEnumerable<KernelIdentities::IdentityId> causedByChain,
        DateTimeOffset occurred,
        ExpandoObject content,
        KernelEvents::EventHash hash) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<KernelAppendedEvent> Redact(
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelEvents::RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<KernelAuditing::Causation> causation,
        IEnumerable<KernelIdentities::IdentityId> causedByChain,
        DateTimeOffset occurred) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEvents::EventType>> Redact(
        KernelEvents::EventSourceId eventSourceId,
        KernelEvents::RedactionReason reason,
        IEnumerable<KernelEvents::EventType>? eventTypes,
        CorrelationId correlationId,
        IEnumerable<KernelAuditing::Causation> causation,
        IEnumerable<KernelIdentities::IdentityId> causedByChain,
        DateTimeOffset occurred) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<KernelEvents::EventSequenceNumber> GetHeadSequenceNumber(IEnumerable<KernelEvents::EventType>? eventTypes = null, KernelEvents::EventSourceId? eventSourceId = null) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<KernelEvents::EventSequenceNumber> GetTailSequenceNumber(
        IEnumerable<KernelEvents::EventType>? eventTypes = null,
        KernelEvents::EventSourceId? eventSourceId = null,
        KernelEvents::EventSourceType? eventSourceType = null,
        KernelEvents::EventStreamId? eventStreamId = null,
        KernelEvents::EventStreamType? eventStreamType = null) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<TailEventSequenceNumbers> GetTailSequenceNumbers(IEnumerable<KernelEvents::EventType> eventTypes) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<IImmutableDictionary<KernelEvents::EventType, KernelEvents::EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(IEnumerable<KernelEvents::EventType> eventTypes) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<KernelEvents::EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(KernelEvents::EventSequenceNumber sequenceNumber, IEnumerable<KernelEvents::EventType>? eventTypes = null, KernelEvents::EventSourceId? eventSourceId = null) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(KernelEvents::EventSourceId eventSourceId) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<Catch<Option<KernelAppendedEvent>>> TryGetLastEventBefore(
        KernelEvents::EventTypeId eventTypeId,
        KernelEvents::EventSourceId eventSourceId,
        KernelEvents::EventSequenceNumber currentSequenceNumber) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<KernelAppendedEvent> GetEventAt(KernelEvents::EventSequenceNumber sequenceNumber) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<Option<KernelAppendedEvent>> TryGetLastInstanceOfAny(KernelEvents::EventSourceId eventSourceId, IEnumerable<KernelEvents::EventTypeId> eventTypes) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<IEventCursor> GetFromSequenceNumber(
        KernelEvents::EventSequenceNumber sequenceNumber,
        KernelEvents::EventSourceId? eventSourceId = default,
        KernelEvents::EventStreamType? eventStreamType = default,
        KernelEvents::EventStreamId? eventStreamId = default,
        IEnumerable<KernelEvents::EventType>? eventTypes = default,
        CancellationToken cancellationToken = default) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<IEventCursor> GetRange(
        KernelEvents::EventSequenceNumber start,
        KernelEvents::EventSequenceNumber end,
        KernelEvents::EventSourceId? eventSourceId = default,
        IEnumerable<KernelEvents::EventType>? eventTypes = default,
        CancellationToken cancellationToken = default) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task<IEventCursor> GetEventsWithLimit(
        KernelEvents::EventSequenceNumber start,
        int limit,
        KernelEvents::EventSourceId? eventSourceId = default,
        KernelEvents::EventStreamType? eventStreamType = default,
        KernelEvents::EventStreamId? eventStreamId = default,
        IEnumerable<KernelEvents::EventType>? eventTypes = default,
        CancellationToken cancellationToken = default) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");

    /// <inheritdoc/>
    public Task ReplaceGenerationContent(
        KernelEvents::EventSequenceNumber sequenceNumber,
        IDictionary<KernelEvents::EventTypeGeneration, ExpandoObject> content) => throw new NotSupportedException("NullEventSequenceStorage does not support this operation.");
}
