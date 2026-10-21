// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class a_repairable_publication_sequence : an_event_sequence_with_repairable_indexes
{
    protected override EventSequenceNumber InitialNext => 2;

    protected override Task<EventSequence> CreateEventSequence()
    {
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage, IEventPublicationStorage>();
        _namespaceStorage.GetEventSequence(SequenceId).Returns(_eventSequenceStorage);
        ((IEventPublicationStorage)_eventSequenceStorage).TryGetPublication(Arg.Any<EventPublication>())
            .Returns(_ => Task.FromResult<Option<EventPublicationReceipt>>(new EventPublicationReceipt(0)));
        _eventSequenceStorage.GetEventAt(0).Returns(_ => Task.FromResult(_log.Events[0]));
        return base.CreateEventSequence();
    }

    protected Task<AppendResult> Publish() => _eventSequence.AppendPublication(
        "publication", "fingerprint", new EventToAppend(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, [], new JsonObject()), CorrelationId.New(), [], Identity.System);
}
