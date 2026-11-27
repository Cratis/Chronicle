// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovery_transfers_to_the_floor;

public class and_the_activation_is_lost_before_the_floor_write : given.an_observer_with_a_handover_log
{
    readonly given.held_storage_operation<bool> _pause = new();
    bool _ownedAtTheCut;
    EventSequenceNumber _floorAtTheCut;

    protected virtual int CrashPoint => 0;

    async Task Because()
    {
        var recovering = await SetUpRecovery();
        var old = _observer.FailedPartitionRecovered("P", 11UL, 11UL, recovering);
        if (CrashPoint < 2) await _pause.WaitForStart();
        else await old;
        _ownedAtTheCut = _failedPartitionsStorage.State.IsFailed("P");
        _floorAtTheCut = PersistedObserverState.NextEventSequenceNumber;
        _writes.BeforeWrite = _ => Task.CompletedTask;
        _writes.AfterWrite = _ => Task.CompletedTask;
        await Crash();
        if (CrashPoint < 2)
        {
            _pause.Fail(new IOException("activation lost"));
            try
            {
                await old;
            }
            catch (IOException)
            {
                // The old activation has no successful acknowledgement.
            }
        }
        if (_failedPartitionsStorage.State.IsFailed("P"))
        {
            await _observer.FailedPartitionRecovered("P", 11UL, 11UL, recovering);
        }
        await _silo.RunCatchUpContinuation();
        await _observer.Handle("Q", [Append("Q", 15UL)]);
    }

    [Fact] void should_have_an_episode_or_safe_floor_at_the_cut() => (_ownedAtTheCut || _floorAtTheCut <= 12UL).ShouldBeTrue();
    [Fact] void should_keep_the_episode_until_the_delete_is_confirmed() => _ownedAtTheCut.ShouldEqual(CrashPoint < 2);
    [Fact] void should_deliver_the_append_after_the_retry_read() => _delivered.ShouldContain((EventSequenceNumber)12UL);
    [Fact] void should_deliver_healthy_backlog() => _delivered.ShouldContain((EventSequenceNumber)13UL);
    [Fact] void should_deliver_a_later_live_probe() => _delivered.ShouldContain((EventSequenceNumber)15UL);
    [Fact] void should_finish_recovery() => _failedPartitionsStorage.State.IsFailed("P").ShouldBeFalse();

    async Task<FailedPartitionId> SetUpRecovery()
    {
        Append("P", 11UL);
        await _observer.PartitionFailed("P", 11UL, ["temporarily failed"], string.Empty);
        var episode = _failedPartitionsStorage.State.Partitions.Single().Id;
        await DeliverRange(11UL, 11UL, [event_type], "P");
        Append("P", 12UL);
        await _observer.Handle("Q", [Append("Q", 13UL)]);
        _writes.State = _writes.State with { NextEventSequenceNumber = 14UL };
        await _writes.WriteStateAsync();
        if (CrashPoint == 0) _writes.BeforeWrite = PauseRewind;
        if (CrashPoint == 1) _writes.AfterWrite = PauseRewind;
        return episode;
    }

    Task PauseRewind(ObserverState written) => written.NextEventSequenceNumber <= 12UL ? _pause.Invoke() : Task.CompletedTask;
}
