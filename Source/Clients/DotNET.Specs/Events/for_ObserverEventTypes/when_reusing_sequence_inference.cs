// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Discovery;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.Events.for_ObserverEventTypes;

public class when_reusing_sequence_inference : an_explicit_store
{
    EventSequenceId _first;
    EventSequenceId _repeat;
    EventSequenceId _equivalentStore;
    EventSequenceId _otherStore;
    EventSequenceId _noEvents;
    EventSequenceId _restored;
    EventSequenceId _otherCatalog;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<Recorded>(builder => builder.WithId("alias").ForEventStore("foreign"));
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        var events = _store.EventTypes.All;
        _first = ObserverEventTypes.InferSequence(typeof(Observer), _store.EventTypes, "current", events);
        _repeat = ObserverEventTypes.InferSequence(typeof(Observer), _store.EventTypes, "current", events);
        _equivalentStore = ObserverEventTypes.InferSequence(typeof(Observer), _store.EventTypes, "other-current", events);
        _otherStore = ObserverEventTypes.InferSequence(typeof(Observer), _store.EventTypes, "foreign", events);
        _noEvents = ObserverEventTypes.InferSequence(typeof(Observer), _store.EventTypes, "current", []);
        _restored = ObserverEventTypes.InferSequence(typeof(Observer), _store.EventTypes, "current", events);
        _options = new() { ArtifactIdentityDrift = ArtifactIdentityDriftPolicy.Ignore };
        _options.ExplicitArtifacts.RegisterEventType<Recorded>(builder => builder.WithId("different-alias").ForEventStore("current"));
        var other = CreateStore();
        await other.DiscoverAll();
        _otherCatalog = ObserverEventTypes.InferSequence(typeof(Observer), other.EventTypes, "current", other.EventTypes.All);
    }

    [Fact] void should_infer_the_foreign_inbox() => _first.Value.ShouldEqual("inbox-foreign");
    [Fact] void should_reuse_the_sequence() => _repeat.ShouldBeSame(_first);
    [Fact] void should_not_grow_the_cache_for_undeclared_store_names() => _equivalentStore.ShouldBeSame(_first);
    [Fact] void should_separate_current_stores() => _otherStore.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_check_the_handled_identities() => _noEvents.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_recompute_when_handled_identities_return() => _restored.ShouldEqual(_first);
    [Fact] void should_separate_explicit_catalogs() => _otherCatalog.ShouldEqual(EventSequenceId.Log);

    public record Recorded;
    public class Observer;
}
