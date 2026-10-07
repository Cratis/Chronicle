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

public class when_pattern_capture_subscription_fails : Seeding.for_EventSeeding.given.an_event_seeding_grain
{
    NamespacesReactor _reactor;
    Exception _error;

    void Establish()
    {
        var capture = Substitute.For<IPatternCapture>();
        capture.Subscribe(_key.EventStore, _key.Namespace)
            .Returns(Task.FromException(new EventSeedingIncomplete(_key.EventStore, _key.Namespace)));
        var global = Substitute.For<IResultAwareEventSeeding>();
        var entry = new SeededEventEntry("the-office", "office-opened", "{\"city\":\"Bergen\"}", []);
        global.GetSeededEvents().Returns(new EventSeeds(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>> { [entry.EventTypeId] = [entry] },
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>> { [entry.EventSourceId] = [entry] }));
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForGlobal(_key.EventStore).ToString(), default).Returns(global);
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForNamespace(_key.EventStore, _key.Namespace).ToString(), default).Returns(_grain);
        _reactor = new NamespacesReactor(_grainFactory, capture, NullLogger<NamespacesReactor>.Instance);
    }

    async Task Because() => _error = await Catch.Exception(() => _reactor.Added(new NamespaceAdded(_key.EventStore, _key.Namespace), null!));

    [Fact] void should_not_fail_the_namespace_partition() => _error.ShouldBeNull();
    [Fact] void should_still_append_the_global_seed() => _eventSequence.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IEventSequence.AppendMany)).ShouldEqual(1);
    [Fact] void should_track_the_seed() => TrackedByEventSource.Count().ShouldEqual(1);
}
