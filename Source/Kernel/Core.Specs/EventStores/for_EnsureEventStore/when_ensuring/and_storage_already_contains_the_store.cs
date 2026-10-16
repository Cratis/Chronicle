// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_storage_already_contains_the_store : given.an_ensure_command
{
    void Establish() => _storage.HasEventStore(_name).Returns(true);

    async Task Because()
    {
        await Ensure();
        await Ensure();
    }

    [Fact] async Task should_repair_the_store_once_even_if_a_read_created_it() => await _projections.Received(1).DiscoverAndRegister(_name);
    [Fact] async Task should_keep_server_event_types_current() => await _eventTypes.Received(2).DiscoverAndRegister(_name);
    [Fact] async Task should_keep_default_namespace_reactors_current() => await _reactors.Received(2).DiscoverAndRegister(_name, EventStoreNamespaceName.Default);
    [Fact] async Task should_ensure_the_default_namespace() => await _namespaces.Received(2).EnsureDefault();
    [Fact] void should_record_one_store_added_notification() => _systemSequence.ReceivedCalls().Count(call => call.GetArguments().OfType<EventStoreAdded>().Any()).ShouldEqual(1);
    [Fact] async Task should_mark_initialization_complete_once() => await _store.Received(1).MarkInitialized();
}
