// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_a_window_opens_after_a_range_started_without_one : given.a_window_mismatch
{
    readonly Guid _opened = Guid.NewGuid();

    async Task Because() => await Mismatch(Windows((SiloAddress.Zero, [])), Windows((SiloAddress.Zero, [_opened])));

    [Fact] void should_fail_the_report() => _error.ShouldBeOfExactType<BulkWindowLost>();
    [Fact] void should_leave_the_durable_checkpoint_unchanged() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_the_unconfirmed_position() => _stateStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_name_the_added_window() => Message.ShouldContain($"added {_opened}");
    [Fact] void should_say_no_window_was_expected() => Message.ShouldContain($"silo {SiloAddress.Zero}: expected <none>, returned {_opened}");
    [Fact] void should_not_list_removed_windows() => Message.ShouldContain("removed <none>");
}
