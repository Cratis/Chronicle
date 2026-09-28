// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendedEvent;

public class when_querying_with_named_tags : Specification
{
    IEventSequenceStorage _eventSequence;
    IStorage _storage;
    IEventCompliance _compliance;
    IQueryContextManager _queryContextManager;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _compliance = Substitute.For<IEventCompliance>();
        _queryContextManager = Substitute.For<IQueryContextManager>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var eventStoreNamespace = Substitute.For<IEventStoreNamespaceStorage>();
        _eventSequence = Substitute.For<IEventSequenceStorage>();
        var cursor = Substitute.For<IEventCursor>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventStoreNamespace);
        eventStoreNamespace.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(_eventSequence);
        eventStore.EventTypes.GetFor(Arg.Any<IEnumerable<Concepts.Events.EventType>>()).Returns([]);
        _eventSequence.GetCountMatching(Arg.Any<EventSequenceQueryCriteria>()).Returns(new EventCount(2));
        _eventSequence.GetPage(Arg.Any<EventSequenceQueryCriteria>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventSequenceQuerySort>()).Returns(cursor);
        cursor.MoveNext().Returns(Task.FromResult(false));
        _compliance.Release(Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<IDictionary<Concepts.Events.EventType, EventTypeSchema>>())
            .Returns(Task.FromResult(Array.Empty<Concepts.Events.AppendedEvent>()));
        _queryContextManager.Current.Returns(QueryContext.NotSet);
    }

    async Task Because() => await AppendedEvent.QueryEventsWithNamedTags(
        _storage,
        _compliance,
        new JsonSerializerOptions(),
        _queryContextManager,
        "store",
        "namespace",
        "log",
        [new("account", ["one"])],
        eventSourceId: "source",
        tags: "legacy");

    [Fact] void should_count_using_all_criteria() => _eventSequence.Received(1).GetCountMatching(Arg.Is<EventSequenceQueryCriteria>(_ => Matches(_)));
    [Fact] void should_page_using_all_criteria() => _eventSequence.Received(1).GetPage(Arg.Is<EventSequenceQueryCriteria>(_ => Matches(_)), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<EventSequenceQuerySort>());
    [Fact] void should_publish_the_matching_total() => _queryContextManager.Current.TotalItems.ShouldEqual(2);

    static bool Matches(EventSequenceQueryCriteria criteria) =>
        criteria.EventSourceId?.Value == "source" &&
        criteria.Tags?.Single().Value == "legacy" &&
        criteria.NamedTags?.Single().Name.Value == "account" &&
        criteria.NamedTags?.Single().Values?.Single() == "one";
}
