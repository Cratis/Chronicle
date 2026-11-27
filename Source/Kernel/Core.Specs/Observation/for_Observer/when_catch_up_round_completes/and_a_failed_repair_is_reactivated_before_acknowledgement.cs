// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_failed_repair_is_reactivated_before_acknowledgement : given.an_observer_behind_a_repair_barrier
{
    int _persistedRepairsAfterFirstContinuation;
    string? _secondTurnError;

    void Establish() => GivenHealthySuccessorPartition("Q");

    async Task Because()
    {
        await _observer.PartitionFailed("P", 12UL, ["repair failed"], string.Empty);
        await Crash();
        _observer.SetSubscription(new ObserverSubscription(_observerId, _observerKey, [event_type], typeof(NullObserverSubscriber), SiloAddress.Zero, null));
        await _observer.CatchUp();

        // Reactivation also schedules an independent alert report, which is not part of the repair handover.
        await _silo.RunCatchUpContinuation();
        _persistedRepairsAfterFirstContinuation = PersistedObserverState.PendingCatchUpRepairs!.Count;
        await _observer.Handle("P", [Event(14UL)]);
        _observer.SetSubscription(subscription);
        try
        {
            await _silo.RunCatchUpContinuation();
        }
        catch (InvalidOperationException ex)
        {
            _secondTurnError = ex.Message;
        }

        await _observer.Handle("P", [Event(14UL)]);
    }

    [Fact] void should_schedule_the_successor_turn() => _secondTurnError.ShouldBeNull();
    [Fact] void should_release_the_repair_barrier_durably_without_a_client() => _persistedRepairsAfterFirstContinuation.ShouldEqual(0);
    [Fact] void should_preserve_the_failed_partition_position() => _failedPartitionsStorage.State.Partitions.Single(_ => _.Partition == (Key)"P").LastAttempt.SequenceNumber.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_not_restart_the_failed_handover_job() => _jobsManager.DidNotReceive().Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
    [Fact] void should_acquire_the_successor_from_the_frozen_cursor() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(_ => _.FromEventSequenceNumber == (EventSequenceNumber)14UL));
    [Fact] void should_not_deliver_the_failed_partition_suffix_from_next() => _delivered.ShouldBeEmpty();
}
