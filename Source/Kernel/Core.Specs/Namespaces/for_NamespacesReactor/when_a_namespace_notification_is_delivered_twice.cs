// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Namespaces.for_NamespacesReactor;

public class when_a_namespace_notification_is_delivered_twice : Seeding.for_EventSeeding.given.an_event_seeding_grain
{
    NamespacesReactor _reactor;
    NamespaceAdded _added;

    void Establish()
    {
        _added = new NamespaceAdded(_key.EventStore, _key.Namespace);
        var global = Substitute.For<IResultAwareEventSeeding>();
        var entry = new SeededEventEntry("the-office", "office-opened", "{\"city\":\"Bergen\"}", ["seed"]);
        global.GetSeededEvents().Returns(new EventSeeds(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>> { [entry.EventTypeId] = [entry] },
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>> { [entry.EventSourceId] = [entry] }));
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForGlobal(_key.EventStore).ToString(), default).Returns(global);
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForNamespace(_key.EventStore, _key.Namespace).ToString(), default).Returns(_grain);
        _reactor = new NamespacesReactor(_grainFactory, Substitute.For<IPatternCapture>());
    }

    async Task Because()
    {
        await _reactor.Added(_added, null!);
        await _reactor.Added(_added, null!);
    }

    [Fact] void should_append_the_global_seed_once() => _eventSequence.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IEventSequence.AppendMany)).ShouldEqual(1);
    [Fact] void should_track_the_global_seed_once() => TrackedByEventSource.Count().ShouldEqual(1);
    [Fact] void should_preserve_the_seed_content() => TrackedByEventSource.Single().Content.ShouldEqual("{\"city\":\"Bergen\"}");
    [Fact] void should_preserve_the_seed_tags() => TrackedByEventSource.Single().Tags.ShouldContainOnly("seed");
}
