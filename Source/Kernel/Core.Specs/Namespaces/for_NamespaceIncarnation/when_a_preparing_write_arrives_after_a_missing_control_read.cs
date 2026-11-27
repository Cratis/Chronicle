// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation;

public class when_a_preparing_write_arrives_after_a_missing_control_read : given.an_owner_with_an_unknown_control_write
{
    void Establish() => LoseNextControlReply();

    async Task Because()
    {
        _error = await Catch.Exception(() => _owner.Ensure(_name));
        await DeliverDelayedWriteAndWake();
    }

    [Fact] void should_surface_the_unknown_write_outcome() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_keep_the_wake_while_the_control_row_is_missing() => _pendingWakes.ShouldEqual(["namespace-incarnation-wake:1"]);
    [Fact] void should_recover_the_reserved_identity() => _state.Incarnation.ShouldEqual(_delayed.Incarnation);
    [Fact] void should_open_on_the_durable_wake() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Open);
    [Fact] void should_clear_the_wake_after_terminal_proof() => _terminalWakes.ShouldBeEmpty();
}
