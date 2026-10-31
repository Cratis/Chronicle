// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeBuilder.when_using_registry_identities;

public class and_clients_use_different_aliases : an_explicit_store
{
    EventStore _other;
    ConcurrencyScope _first;
    ConcurrencyScope _second;
    ConcurrencyScope _copy;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("first").Generation(2));
        _store = CreateStore();
        _options = new();
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("second").Generation(4));
        _other = CreateStore();
    }

    async Task Because()
    {
        await _other.DiscoverAll();
        await _store.DiscoverAll();
        _first = new ConcurrencyScopeBuilder().WithEventType(_store.EventTypes.GetEventTypeFor(typeof(PlainEvent))).Build();
        _second = new ConcurrencyScopeBuilder().WithEventTypes(_other.EventTypes.All).Build();
        _copy = _first with { SequenceNumber = (EventSequenceNumber)12 };
    }

    [Fact] void should_use_the_first_registered_identity() => _first.EventTypes.ShouldContainOnly(new EventType("first", 2));
    [Fact] void should_use_the_second_registered_identity() => _second.EventTypes.ShouldContainOnly(new EventType("second", 4));
    [Fact] void should_preserve_resolved_identities_on_copy() => _copy.EventTypes.ShouldContainOnly(new EventType("first", 2));
}
