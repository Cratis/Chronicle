// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Sequences.for_ExportedEvent;

public class when_exporting_an_event_with_named_tags : Specification
{
    ExportedEvent _result;
    IStorage _storage;
    IEventCompliance _compliance;
    IQueryContextManager _queryContextManager;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var eventStoreNamespace = Substitute.For<IEventStoreNamespaceStorage>();
        var eventSequence = Substitute.For<IEventSequenceStorage>();
        var eventTypes = Substitute.For<IEventTypesStorage>();
        var compliance = Substitute.For<IEventCompliance>();
        var queryContextManager = Substitute.For<IQueryContextManager>();
        var cursor = Substitute.For<IEventCursor>();

        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventStoreNamespace);
        eventStoreNamespace.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(eventSequence);
        eventStore.EventTypes.Returns(eventTypes);
        eventSequence.GetCountMatching(Arg.Any<EventSequenceQueryCriteria>()).Returns(Task.FromResult(new EventCount(1)));
        eventTypes.GetFor(Arg.Any<IEnumerable<Concepts.Events.EventType>>()).Returns(Task.FromResult<IEnumerable<EventTypeSchema>>([]));

        var context = Concepts.Events.EventContext.EmptyWithEventSourceId("source-id") with
        {
            NamedTags = [new Concepts.Events.NamedTag(new TagName("region"), "north")]
        };
        var appendedEvent = new Concepts.Events.AppendedEvent(context, new ExpandoObject());
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns([appendedEvent]);
        eventSequence.GetPage(Arg.Any<EventSequenceQueryCriteria>(), 0, int.MaxValue, Arg.Any<EventSequenceQuerySort>()).Returns(cursor);
        compliance.Release(Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<IDictionary<Concepts.Events.EventType, EventTypeSchema>>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<Concepts.Events.AppendedEvent>>(0).ToArray()));
        queryContextManager.Current.Returns(QueryContext.NotSet);

        _storage = storage;
        _compliance = compliance;
        _queryContextManager = queryContextManager;
    }

    async Task Because() => _result = (await ExportedEvent.ExportEvents(
        _storage,
        _compliance,
        new JsonSerializerOptions(),
        _queryContextManager,
        "store",
        "namespace",
        "event-log")).Single();

    [Fact] void should_export_the_named_tag_name() => _result.NamedTags.Single().Name.ShouldEqual("region");
    [Fact] void should_export_the_named_tag_value() => _result.NamedTags.Single().Value.ShouldEqual("north");
}
