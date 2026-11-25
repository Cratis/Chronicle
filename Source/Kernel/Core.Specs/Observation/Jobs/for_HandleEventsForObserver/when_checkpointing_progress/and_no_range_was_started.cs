// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_no_range_was_started : given.a_window_mismatch
{
    readonly Guid _window = Guid.NewGuid();

    async Task Because() => await Mismatch(Windows((SiloAddress.Zero, [_window])), Windows((SiloAddress.Zero, [_window])), startFirst: false);

    [Fact] void should_fail_the_report() => _error.ShouldBeOfExactType<BulkWindowLost>();
    [Fact] void should_leave_the_durable_checkpoint_unchanged() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_the_unconfirmed_position() => _stateStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_say_the_baseline_is_absent() => Message.ShouldContain("baseline absent");
    [Fact] void should_say_the_pin_age_is_unknown() => Message.ShouldContain("pin age unknown");
    [Fact] void should_name_the_window_as_added() => Message.ShouldContain($"added {_window}");
}
