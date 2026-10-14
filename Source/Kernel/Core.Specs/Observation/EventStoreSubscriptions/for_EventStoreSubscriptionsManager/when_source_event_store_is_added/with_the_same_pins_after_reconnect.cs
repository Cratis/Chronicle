// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_source_event_store_is_added;

public class with_the_same_pins_after_reconnect : given.a_manager_with_pinned_generations
{
    async Task Because() => await _manager.SourceEventStoreAdded("Admin");
    [Fact] void should_not_rebind_or_refresh_pins() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name.StartsWith("Subscribe", StringComparison.Ordinal)).ShouldEqual(0);
    [Fact] void should_not_unsubscribe() => _observer.DidNotReceive().Unsubscribe();
}
