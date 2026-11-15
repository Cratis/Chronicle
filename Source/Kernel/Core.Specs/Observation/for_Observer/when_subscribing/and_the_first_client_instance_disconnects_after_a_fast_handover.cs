// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_the_first_client_instance_disconnects_after_a_fast_handover : given.an_observer_served_by_one_client_instance
{
    ObserverSubscription _subscription;

    async Task Establish()
    {
        await SubscribeSecondClient();
        _storageStats.ResetCounts();
        _definitionStorageStats.ResetCounts();
    }

    async Task Because()
    {
        await _observer.UnsubscribeIfMatchesClient(_firstClient.ConnectionId);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_only_target_the_second_client_instance() => _subscription.Targets.Select(target => target.ConnectedClient!.ConnectionId).ShouldContainOnly(_secondClient.ConnectionId);
    [Fact] void should_not_write_observer_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_the_definition() => _definitionStorageStats.Writes.ShouldEqual(0);
}
