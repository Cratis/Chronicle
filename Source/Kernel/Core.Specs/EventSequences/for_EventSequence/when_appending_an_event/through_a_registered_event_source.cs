// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventSources;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventSources;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class through_a_registered_event_source : given.an_event_sequence
{
    AppendResult _result;
    IEnumerable<EventToAppendToStorage> _stored;

    void Establish()
    {
        var eventSources = Substitute.For<IEventSourcesStorage>();
        eventSources.Find(new EventSourceName("ShoppingCart")).Returns(new EventSourceDefinition(
            "ShoppingCart",
            string.Empty,
            EventSourceOwner.Client,
            ConcurrencyDimensions.None,
            [new EventStreamDefinition("Items", string.Empty, ConcurrencyDimensions.None)]));
        _eventStoreStorage.EventSources.Returns(eventSources);
        _eventSequenceStorage.AppendMany(Arg.Do<IEnumerable<EventToAppendToStorage>>(events => _stored = events.ToArray()))
            .Returns(callInfo => Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(
                callInfo.Arg<IEnumerable<EventToAppendToStorage>>().Select(_ => new AppendedEvent(
                    EventContext.From(EventStore, EventStoreNamespace, _.EventType, _.EventSourceType, _.EventSourceId, _.EventStreamType, _.EventStreamId, _.SequenceNumber, CorrelationId.NotSet),
                    new System.Dynamic.ExpandoObject())).ToArray())));
    }

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        new EventStreamType("Items"),
        EventStreamId.Default,
        _eventType,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None,
        null,
        null,
        [],
        eventSource: new EventSourceName("ShoppingCart"));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_the_event_source_name() => _stored.Single().EventSource.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_store_the_event_source_type_from_the_definition() => _stored.Single().EventSourceType.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_store_the_stream_type() => _stored.Single().EventStreamType.Value.ShouldEqual("Items");
}
