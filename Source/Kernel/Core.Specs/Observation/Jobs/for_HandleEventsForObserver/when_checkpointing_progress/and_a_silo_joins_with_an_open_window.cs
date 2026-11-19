// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_checkpointing_progress;

public class and_a_silo_joins_with_an_open_window : given.a_window_mismatch
{
    readonly Guid _window = Guid.NewGuid();
    readonly Guid _joined = Guid.NewGuid();

    async Task Because() => await Mismatch(
        Windows((SiloAddress.Zero, [_window])),
        Windows((SiloAddress.Zero, [_window]), (_otherSilo, [_joined])));

    [Fact] void should_fail_the_report() => _error.ShouldBeOfExactType<BulkWindowLost>();
    [Fact] void should_leave_the_durable_checkpoint_unchanged() => _stateStorage.State.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_the_unconfirmed_position() => _stateStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_name_the_silo_as_new() => Message.ShouldContain($"new silos {_otherSilo}");
    [Fact] void should_name_the_added_window() => Message.ShouldContain($"added {_joined}");
    [Fact] void should_not_report_a_missing_silo() => Message.ShouldContain("missing silos <none>");
}
