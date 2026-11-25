// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_a_window_is_replaced : given.a_window_mismatch
{
    readonly Guid _pinned = Guid.NewGuid();
    readonly Guid _returned = Guid.NewGuid();

    void Establish() => _performState.EventObservationState = EventObservationState.None;

    async Task Because() => await Mismatch(Windows((SiloAddress.Zero, [_pinned])), Windows((SiloAddress.Zero, [_returned])));

    [Fact] void should_fail_the_report() => _error.ShouldBeOfExactType<BulkWindowLost>();
    [Fact] void should_say_the_windows_were_not_returned_by_the_confirmation() => Message.ShouldContain("not returned by confirmation");
    [Fact] void should_name_the_observer() => Message.ShouldContain("observer-id");
    [Fact] void should_leave_the_durable_checkpoint_unchanged() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_the_unconfirmed_position() => _stateStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_name_the_expected_and_returned_windows_of_the_silo() => Message.ShouldContain($"silo {SiloAddress.Zero}: expected {_pinned}, returned {_returned}");
    [Fact] void should_name_the_removed_window() => Message.ShouldContain($"removed {_pinned}");
    [Fact] void should_name_the_added_window() => Message.ShouldContain($"added {_returned}");
    [Fact] void should_say_it_was_a_catch_up() => Message.ShouldContain("mode catch-up");
    [Fact] void should_name_the_event_store_and_namespace() => Message.ShouldContain("(event-store/event-store-namespace)");
    [Fact] void should_name_the_handled_position() => Message.ShouldContain("handled 3");
    [Fact] void should_say_the_baseline_is_present() => Message.ShouldContain("baseline present");
}
