// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_a_pinned_silo_disappears : given.a_window_mismatch
{
    readonly Guid _pinned = Guid.NewGuid();

    async Task Because() => await Mismatch(Windows((SiloAddress.Zero, [_pinned])), Windows());

    [Fact] void should_fail_the_report() => _error.ShouldBeOfExactType<BulkWindowLost>();
    [Fact] void should_leave_the_durable_checkpoint_unchanged() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_the_unconfirmed_position() => _stateStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_name_the_silo_as_missing() => Message.ShouldContain($"missing silos {SiloAddress.Zero}");
    [Fact] void should_name_the_removed_window() => Message.ShouldContain($"removed {_pinned}");
    [Fact] void should_say_nothing_was_returned_for_the_silo() => Message.ShouldContain($"silo {SiloAddress.Zero}: expected {_pinned}, returned <none>");
}
