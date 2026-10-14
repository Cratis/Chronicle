// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_waiting_until_subscribed;

public class with_stale_generation_pins : given.a_manager_with_pinned_generations
{
    Exception _error;
    void Establish() => _currentPins = [new("customer", 2)];
    async Task Because() => _error = await Catch.Exception(() => _manager.WaitUntilSubscribed("subscription", TimeSpan.Zero));
    [Fact] void should_not_report_readiness_for_a_stale_generation() => _error.ShouldBeOfExactType<TimeoutException>();
}
