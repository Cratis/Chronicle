// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverIds;

public class when_reading_the_target_event_store : Specification
{
    bool _found;
    EventStoreName _target;
    bool _foundInPlainId;

    void Because()
    {
        _found = EventStoreSubscriptionObserverIds.TryGetTargetEventStore(EventStoreSubscriptionObserverIds.For("source", "target"), out _target);
        _foundInPlainId = EventStoreSubscriptionObserverIds.TryGetTargetEventStore(new("a-client-reactor-id"), out _);
    }

    [Fact] void should_find_the_target_in_an_id_it_made() => _found.ShouldBeTrue();
    [Fact] void should_read_the_target() => _target.Value.ShouldEqual("target");
    [Fact] void should_not_find_a_target_in_an_id_without_one() => _foundInPlainId.ShouldBeFalse();
}
