// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_waiting_until_subscribed;

public class and_restricted : given.a_manager_over_several_namespaces
{
    Exception _error;

    async Task Establish()
    {
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor("Tenant1"));
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.WaitUntilSubscribed(SubscriptionName, TimeSpan.FromSeconds(5)));

    [Fact] void should_be_ready() => _error.ShouldBeNull();
    [Fact] void should_only_check_the_listed_namespace() => RequestedKeys.ShouldContainOnly(KeyFor("Tenant1"));
}
