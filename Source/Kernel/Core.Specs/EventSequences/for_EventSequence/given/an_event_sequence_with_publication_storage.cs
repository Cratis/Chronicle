// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_publication_storage : appending_many_events
{
    protected static readonly EventSequenceNumber DurableSlot = 7;

    protected IEventPublicationStorage _publicationStorage;

    protected override async Task<EventSequence> CreateEventSequence()
    {
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage, IEventPublicationStorage>();
        _publicationStorage = (IEventPublicationStorage)_eventSequenceStorage;
        _namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);
        _eventSequenceStorage.GetEventAt(DurableSlot).Returns(_ => Task.FromResult(new AppendedEvent(
            EventContext.From(EventStore, EventStoreNamespace, _eventType, EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, DurableSlot, CorrelationId.NotSet),
            new ExpandoObject())));
        return await base.CreateEventSequence();
    }

    protected Task<AppendResult> Publish() => _eventSequence.AppendPublication(
        "publication-1",
        "fingerprint-1",
        EventToAppendFor(_eventSourceId),
        CorrelationId.New(),
        [],
        Identity.System);
}
