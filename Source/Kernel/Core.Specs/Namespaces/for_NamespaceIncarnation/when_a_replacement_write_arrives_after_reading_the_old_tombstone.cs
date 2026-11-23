// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation;

public class when_a_replacement_write_arrives_after_reading_the_old_tombstone : given.an_owner_with_an_unknown_control_write
{
    void Establish()
    {
        _state = new(_name, NamespaceIncarnationId.New(), _clock.GetUtcNow()) { Status = NamespaceLifecycleStatus.Removed, Revision = 10 };
        LoseNextControlReply();
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _owner.Ensure(_name));
        await DeliverDelayedWriteAndWake();
    }

    [Fact] void should_keep_the_replacement_wake_despite_the_old_terminal_row() => _pendingWakes.ShouldEqual(["namespace-incarnation-wake:11"]);
    [Fact] void should_recover_the_replacement_identity() => _state.Incarnation.ShouldEqual(_delayed.Incarnation);
    [Fact] void should_open_the_replacement() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Open);
    [Fact] void should_clear_the_wake_after_the_replacement_is_open() => _terminalWakes.ShouldBeEmpty();
}
