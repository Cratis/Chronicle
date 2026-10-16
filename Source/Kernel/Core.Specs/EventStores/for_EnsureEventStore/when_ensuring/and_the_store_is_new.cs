// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_the_store_is_new : given.an_ensure_command
{
    Task Because() => Ensure();

    [Fact] async Task should_register_kernel_projections() => await _projections.Received(1).DiscoverAndRegister(_name);
    [Fact] async Task should_ensure_the_default_namespace() => await _namespaces.Received(1).EnsureDefault();
    [Fact] void should_emit_one_store_added_event() => _systemSequence.ReceivedCalls().Count(call => call.GetArguments().OfType<EventStoreAdded>().Any(added => added.EventStore == _name)).ShouldEqual(1);
}
