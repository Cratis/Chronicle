// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

public partial class EventSequenceStorage
{
    readonly Dictionary<string, (string Fingerprint, EventPublicationReceipt Receipt)> _publications = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public Task<Option<EventPublicationReceipt>> TryGetPublication(EventPublication publication)
    {
        lock (_lock)
        {
            return Task.FromResult(FindPublication(publication));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>> AppendPublication(EventPublication publication, EventToAppendToStorage @event)
    {
        var causedBy = await identityStorage.GetFor(@event.CausedByChain).ConfigureAwait(false);
        lock (_lock)
        {
            if (FindPublication(publication).TryGetValue(out var existing))
            {
                return existing;
            }

            if (_events.Exists(_ => _.Context.SequenceNumber == @event.SequenceNumber))
            {
                return new DuplicateEventSequenceNumber((EventSequenceNumber)(_events.Max(_ => _.Context.SequenceNumber.Value) + 1));
            }

            var hash = @event.ContentHashes.TryGetValue(@event.EventType.Generation, out var contentHash) ? contentHash : EventHash.NotSet;
            var appended = BuildAppendedEvent(
                @event.SequenceNumber,
                @event.EventSourceType,
                @event.EventSourceId,
                @event.EventStreamType,
                @event.EventStreamId,
                @event.EventType,
                @event.CorrelationId,
                @event.Causation,
                causedBy,
                @event.Tags,
                @event.Occurred,
                @event.GenerationalContent,
                hash,
                @event.Subject,
                @event.NamedTags,
                @event.EventSource);
            var receipt = new EventPublicationReceipt(@event.SequenceNumber);
            _events.Add(appended);
            _originalCausedByChains[@event.SequenceNumber] = @event.CausedByChain.ToArray();
            TrackMetadata(appended, _originalCausedByChains[@event.SequenceNumber]);
            _appendedGenerations[@event.SequenceNumber] = @event.EventType.Generation.Value;
            _publications.Add(publication.Id, (publication.Fingerprint, receipt));
            return receipt;
        }
    }

    Option<EventPublicationReceipt> FindPublication(EventPublication publication)
    {
        if (!_publications.TryGetValue(publication.Id, out var existing))
        {
            return Option<EventPublicationReceipt>.None();
        }

        if (!string.Equals(existing.Fingerprint, publication.Fingerprint, StringComparison.Ordinal))
        {
            throw new EventPublicationConflict();
        }

        return existing.Receipt;
    }
}
