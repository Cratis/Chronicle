// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_storage_already_contains_the_store : given.an_ensure_command
{
    void Establish() => _storage.HasEventStore(_name).Returns(true);

    Task Because() => Ensure();

    [Fact] async Task should_reconcile_kernel_projections_even_if_a_read_created_the_store() => await _projections.Received(1).DiscoverAndRegister(_name);
    [Fact] async Task should_register_server_event_types() => await _eventTypes.Received(1).DiscoverAndRegister(_name);
    [Fact] async Task should_register_default_namespace_reactors() => await _reactors.Received(1).DiscoverAndRegister(_name, EventStoreNamespaceName.Default);
    [Fact] async Task should_ensure_the_default_namespace() => await _namespaces.Received(1).EnsureDefault();
    [Fact] void should_not_emit_another_store_added_event() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
}
