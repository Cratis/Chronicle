// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_waiting_until_subscribed;

public class and_a_target_namespace_has_no_counterpart_in_the_source : given.a_manager_over_several_namespaces
{
    Exception? _error;

    async Task Establish()
    {
        _sourceHas.Remove("Tenant2");
        _observersAreSubscribed = true;
        await _manager.Add(DefinitionFor());
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.WaitUntilSubscribed(SubscriptionName, TimeSpan.FromSeconds(1)));

    [Fact] void should_not_wait_for_a_namespace_the_source_does_not_have() => _error.ShouldBeNull();
    [Fact] void should_never_resolve_an_observer_that_would_create_the_namespace_in_the_source() => RequestedKeys.ShouldNotContain(KeyFor("Tenant2"));
}
