// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public Task<AppendedEvent> Redact(
        EventSequenceNumber sequenceNumber,
        RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        lock (_lock)
        {
            var index = _events.FindIndex(_ => _.Context.SequenceNumber == sequenceNumber);
            if (index < 0)
            {
                throw new NoEventAtSequenceNumber(eventSequenceId, sequenceNumber);
            }

            var original = _events[index];
            if (original.Context.EventType.Id == GlobalEventTypes.Redaction)
            {
                return Task.FromResult(original);
            }

            RedactAt(index, original, new RedactionRequest(reason, correlationId, causation, causedByChain, occurred));
            return Task.FromResult(original);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventType>> Redact(
        EventSourceId eventSourceId,
        RedactionReason reason,
        IEnumerable<EventType>? eventTypes,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        var affectedEventTypes = new HashSet<EventType>();
        var eventTypeIds = eventTypes?.Select(_ => _.Id).ToHashSet();
        var request = new RedactionRequest(reason, correlationId, causation, causedByChain, occurred);

        lock (_lock)
        {
            for (var index = 0; index < _events.Count; index++)
            {
                var original = _events[index];
                if (!IsRedactionTarget(original, eventSourceId, eventTypeIds))
                {
                    continue;
                }

                affectedEventTypes.Add(new EventType(original.Context.EventType.Id, EventTypeGeneration.First, false));
                RedactAt(index, original, request);
            }
        }

        return Task.FromResult<IEnumerable<EventType>>(affectedEventTypes);
    }

    /// <summary>
    /// Produces the redacted replacement for an event, mirroring how the persistent providers redact in place:
    /// the event type becomes <see cref="GlobalEventTypes.Redaction"/>, the payload is replaced with a
    /// <see cref="RedactionEventContent"/> describing what was redacted, and the auditing fields become the
    /// redaction's. The sequence number and event source are preserved.
    /// </summary>
    /// <param name="original">The event being redacted.</param>
    /// <param name="request">The <see cref="RedactionRequest"/> describing the redaction.</param>
    /// <param name="originalCausedBy">The original caused-by identity chain.</param>
    /// <returns>The redacted <see cref="AppendedEvent"/>.</returns>
    static AppendedEvent Redacted(
        AppendedEvent original,
        RedactionRequest request,
        IEnumerable<IdentityId> originalCausedBy)
    {
        var content = RedactionEventContent.FromOriginal(
            request.Reason,
            original.Context.EventType.Id,
            original.Context.Occurred,
            original.Context.CorrelationId,
            original.Context.Causation,
            originalCausedBy);

        return original with
        {
            Context = original.Context with
            {
                EventType = new EventType(GlobalEventTypes.Redaction, EventTypeGeneration.First, false),
                Occurred = request.Occurred,
                CorrelationId = request.CorrelationId,
                Causation = request.Causation,
                CausedBy = Identity.System,
                Hash = EventHash.NotSet
            },
            Content = content.ToPayload(),
            OriginalContent = string.Empty,
            Revisions = [],
            GenerationalContent = new Dictionary<int, string>()
        };
    }

    static bool IsRedactionTarget(AppendedEvent candidate, EventSourceId eventSourceId, HashSet<EventTypeId>? eventTypeIds) =>
        candidate.Context.EventSourceId == eventSourceId &&
        candidate.Context.EventType.Id != GlobalEventTypes.Redaction &&
        !(eventTypeIds?.Count > 0 && !eventTypeIds.Contains(candidate.Context.EventType.Id));

    void RedactAt(int index, AppendedEvent original, RedactionRequest request)
    {
        var number = original.Context.SequenceNumber;
        _events[index] = Redacted(original, request, _originalCausedByChains[number]);
        _originalCausedByChains.Remove(number);
        RedactMetadata(number, request);
        _generationHashes.Remove(number);
        _derivedGenerations.Remove(number);
    }

    void RedactMetadata(EventSequenceNumber number, RedactionRequest request)
    {
        _metadata[number] = _metadata[number] with
        {
            EventTypeId = GlobalEventTypes.Redaction,
            Occurred = request.Occurred,
            CorrelationId = request.CorrelationId,
            Causation = SnapshotCausation(request.Causation),
            CausedByChain = request.CausedByChain.ToImmutableArray()
        };
    }

    /// <summary>
    /// Describes a redaction to apply to an event.
    /// </summary>
    /// <param name="Reason">The <see cref="RedactionReason"/>.</param>
    /// <param name="CorrelationId">The <see cref="CorrelationId"/> of the redaction.</param>
    /// <param name="Causation">The causation chain behind the redaction.</param>
    /// <param name="CausedByChain">The identity chain behind the redaction.</param>
    /// <param name="Occurred">When the redaction occurred.</param>
    readonly record struct RedactionRequest(
        RedactionReason Reason,
        CorrelationId CorrelationId,
        IEnumerable<Causation> Causation,
        IEnumerable<IdentityId> CausedByChain,
        DateTimeOffset Occurred);
}
