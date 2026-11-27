// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovery_transfers_to_the_floor;

public class and_a_stale_retry_reports_success_or_partial_progress : given.an_observer_with_a_handover_log
{
    FailedPartitionId _current;
    EventSequenceNumber _floor;

    async Task Because()
    {
        await _observer.PartitionFailed("P", 11UL, ["old failure"], string.Empty);
        var stale = _failedPartitionsStorage.State.Partitions.Single().Id;
        await _observer.ClearFailedPartitions();
        await _observer.PartitionFailed("P", 19UL, ["new failure"], string.Empty);
        _current = _failedPartitionsStorage.State.Partitions.Single().Id;
        _floor = PersistedObserverState.NextEventSequenceNumber;
        await _observer.FailedPartitionPartiallyRecovered("P", 20UL, stale);
        await _observer.FailedPartitionPartiallyRecovered("P", 21UL);
        await _observer.FailedPartitionRecovered("P", 22UL, 22UL, stale);
        await _observer.FailedPartitionRecovered("P", 23UL, 23UL);
    }

    [Fact] void should_keep_the_current_episode() => _failedPartitionsStorage.State.Partitions.Select(failure => failure.Id).ShouldContainOnly(_current);
    [Fact] void should_keep_its_resume_position() => _failedPartitionsStorage.State.Partitions.Single().Attempts.Select(attempt => attempt.SequenceNumber)
        .ShouldContainOnly((EventSequenceNumber)19UL);
    [Fact] void should_not_move_the_floor() => PersistedObserverState.NextEventSequenceNumber.ShouldEqual(_floor);
}
