// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation;

public class when_a_closing_write_arrives_after_reading_the_old_open_row : given.an_owner_with_an_unknown_control_write
{
    void Establish()
    {
        _state = NamespaceLifecycle.Open(_name, NamespaceIncarnationId.New(), _clock.GetUtcNow()) with { Revision = 10 };
        _markerState = new(_state.Incarnation, true, 0);
        LoseNextControlReply();
    }

    async Task Because()
    {
        _error = await Catch.Exception(_owner.Remove);
        await DeliverDelayedWriteAndWake();
    }

    [Fact] void should_keep_the_removal_wake_despite_the_old_open_row() => _pendingWakes.ShouldEqual(["namespace-incarnation-wake:11"]);
    [Fact] void should_finish_the_original_removal_operation() => _state.OperationId.ShouldEqual(_delayed.OperationId);
    [Fact] void should_finish_removal_on_the_durable_wake() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Removed);
    [Fact] void should_clear_the_wake_only_after_the_tombstone_is_written() => _terminalWakes.ShouldBeEmpty();
}
