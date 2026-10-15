// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Storage.Seeding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Namespaces.for_NamespacesReactor;

public class when_a_namespace_is_added_with_routed_global_seeds : Seeding.for_EventSeeding.given.an_event_seeding_grain
{
    NamespacesReactor _reactor;
    EventToAppend _appended;

    void Establish()
    {
        var global = Substitute.For<IResultAwareEventSeeding>();
        var entry = new SeededEventEntry("source", "type", "{}", ["seed"])
        {
            EventSourceType = "Order",
            EventStreamType = "Lines",
            EventStreamId = "line-1"
        };
        global.GetSeededEvents().Returns(new EventSeeds(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>> { [entry.EventTypeId] = [entry] },
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>> { [entry.EventSourceId] = [entry] }));
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForGlobal(_key.EventStore).ToString(), default).Returns(global);
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForNamespace(_key.EventStore, _key.Namespace).ToString(), default).Returns(_grain);
        _reactor = new NamespacesReactor(_grainFactory, Substitute.For<IPatternCapture>(), NullLogger<NamespacesReactor>.Instance);
    }

    async Task Because()
    {
        await _reactor.Added(new NamespaceAdded(_key.EventStore, _key.Namespace), null!);
        _appended = ((IEnumerable<EventToAppend>)_eventSequence.ReceivedCalls().Single().GetArguments()[0]).Single();
    }

    [Fact] void should_append_with_the_source_type() => _appended.EventSourceType.Value.ShouldEqual("Order");
    [Fact] void should_append_with_the_stream_type() => _appended.eventStreamType.Value.ShouldEqual("Lines");
    [Fact] void should_append_with_the_stream_id() => _appended.eventStreamId.Value.ShouldEqual("line-1");
    [Fact] void should_track_the_routed_global_seed_in_the_namespace() => TrackedByEventSource.Single().EventStreamId.Value.ShouldEqual("line-1");
}
