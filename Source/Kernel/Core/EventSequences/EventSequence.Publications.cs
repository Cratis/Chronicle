// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <inheritdoc/>
    public Task<AppendResult> AppendPublication(
        string publicationId,
        string intentFingerprint,
        EventToAppend @event,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy) =>
        AppendValidated(
            @event.EventSourceType,
            @event.EventSourceId,
            @event.eventStreamType,
            @event.eventStreamId,
            @event.EventType,
            @event.Content,
            correlationId,
            @event.Causation ?? causation,
            causedBy,
            @event.Tags,
            ConcurrencyScope.None,
            @event.Occurred,
            @event.Subject,
            @event.NamedTags,
            @event.EventSource,
            new EventPublication(publicationId, intentFingerprint));

    async Task<Result<Concepts.Events.AppendedEvent, DuplicateEventSequenceNumber>> AppendPublicationToStorage(EventPublication publication, EventToAppendToStorage @event)
    {
        var publicationStorage = EventSequenceStorage as IEventPublicationStorage ?? throw new EventPublicationStorageNotSupported();
        var result = await publicationStorage.AppendPublication(publication, @event);
        if (result.TryGetError(out var conflict))
        {
            return conflict;
        }

        // The receipt may refer to a slot allocated by an earlier activation or a competing silo.
        // Never substitute the proposed slot for the event that is actually durable.
        return await EventSequenceStorage.GetEventAt(result.AsT0.SequenceNumber);
    }
}
