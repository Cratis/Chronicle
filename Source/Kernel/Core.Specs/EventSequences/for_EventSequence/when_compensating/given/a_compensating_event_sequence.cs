// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_compensating.given;

public class a_compensating_event_sequence : for_EventSequence.given.appending_many_events
{
    protected static readonly EventSequenceNumber OriginalSequenceNumber = 3UL;

    protected EventType _originalEventType;
    protected EventType _compensatingEventType;
    protected EventToAppendToStorage[] _stored;

    void Establish()
    {
        _originalEventType = new EventType("original-event", EventTypeGeneration.First);
        _compensatingEventType = new EventType("compensating-event", EventTypeGeneration.First);
        _stored = [];

        _eventSequenceStorage.GetTailSequenceNumber().Returns((EventSequenceNumber)5UL);
        _eventSequenceStorage.GetEventAt(OriginalSequenceNumber).Returns(Task.FromResult(new AppendedEvent(
            EventContext.From(
                EventStore,
                EventStoreNamespace,
                _originalEventType,
                "Account",
                "account-1",
                "Payments",
                "September",
                OriginalSequenceNumber,
                CorrelationId.New(),
                subject: new Subject("person-1")),
            new ExpandoObject())));

        SchemaCompensating(_compensatingEventType, _originalEventType);

        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call =>
            {
                _stored = call.Arg<IEnumerable<EventToAppendToStorage>>().ToArray();
                return Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(AppendedEventsFrom(_stored)));
            });
    }

    protected void SchemaCompensating(EventType compensating, EventType compensated)
    {
        var schema = new JsonSchema();
        schema.SetCompensationFor(compensated.Id.Value);
        _eventTypesStorage.GetFor(compensating.Id, Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(compensating, EventTypeOwner.Client, EventTypeSource.Code, schema));
    }

    protected Task<AppendResult> Compensate(EventSequenceNumber compensationFor) => _eventSequence.Compensate(
        compensationFor,
        _compensatingEventType,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        []);
}
