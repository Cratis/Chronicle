// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_the_store_is_initialized : given.an_ensure_command
{
    void Establish() => _initialized = true;

    Task Because() => Ensure();

    [Fact] void should_not_touch_kernel_projections() => _projections.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_repeat_store_added_notification() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_not_write_the_marker_again() => await _store.DidNotReceive().MarkInitialized();
}
