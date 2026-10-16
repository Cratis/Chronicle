// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_another_client_completes_initialization_first : given.an_ensure_command
{
    void Establish() => _store.IsInitialized().Returns(false, true);

    Task Because() => Ensure();

    [Fact] async Task should_recheck_the_marker_inside_the_exclusive_turn() => await _store.Received(2).IsInitialized();
    [Fact] async Task should_enter_the_initializer_after_the_initial_read() => await _initializerReference.Received(1).Ensure();
    [Fact] void should_not_register_projections_again() => _projections.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_repeat_the_notification() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_not_write_the_marker_again() => await _store.DidNotReceive().MarkInitialized();
}
