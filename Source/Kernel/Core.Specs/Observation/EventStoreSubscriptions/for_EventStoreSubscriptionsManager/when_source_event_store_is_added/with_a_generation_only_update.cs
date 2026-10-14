// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_source_event_store_is_added;

public class with_a_generation_only_update : given.a_manager_with_pinned_generations
{
    void Establish() => _currentPins = [new("customer", 2)];
    async Task Because() => await _manager.SourceEventStoreAdded("Admin");
    [Fact] void should_rebind_to_the_exact_generation() => ShouldSubscribePinned();
    [Fact] void should_not_create_an_unsubscribe_gap() => _observer.DidNotReceive().Unsubscribe();
}
