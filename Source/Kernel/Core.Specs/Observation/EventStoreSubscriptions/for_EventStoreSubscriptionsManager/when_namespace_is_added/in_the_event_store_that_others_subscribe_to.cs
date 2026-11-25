// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_namespace_is_added;

public class in_the_event_store_that_others_subscribe_to : given.a_manager_over_several_namespaces
{
    async Task Establish() => await AttachNamespaceAdded();

    async Task Because() => await _onNamespaceAdded(new("Target", "Tenant9"));

    IEventStoreSubscriptionsManager ManagerOf(string eventStore) => _otherManagers.Single(pair => pair.Key.Contains(eventStore, StringComparison.Ordinal)).Value;

    [Fact] void should_tell_the_first_other_event_store_to_look_again() => ManagerOf("Source").Received(1).SourceEventStoreAdded("Target");
    [Fact] void should_tell_the_second_other_event_store_to_look_again() => ManagerOf("Other").Received(1).SourceEventStoreAdded("Target");
    [Fact] void should_not_tell_itself() => _otherManagers.Keys.Any(key => key.Contains("Target", StringComparison.Ordinal)).ShouldBeFalse();
}
