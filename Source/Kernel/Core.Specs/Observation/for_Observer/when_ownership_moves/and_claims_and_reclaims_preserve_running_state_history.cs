// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_claims_and_reclaims_preserve_running_state_history : given.a_live_observer_with_guarded_state
{
    readonly DateTimeOffset _entryTime = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    readonly List<(ObserverState State, DateTimeOffset? ChangedAt)> _claimed = [];
    readonly List<(ObserverState State, DateTimeOffset? ChangedAt)> _reclaimed = [];

    async Task Because()
    {
        foreach (var timestamp in new DateTimeOffset?[] { _entryTime, null })
        {
            await _guarded.Save(_stateStorage.State, _observerKey.EventSequenceId);
            await _guarded.SaveRunningStateChangedAt(_observerId, timestamp, _observerKey.EventSequenceId);
            await _guarded.ClaimOwnership(_observerId, "another-sequence");
            _claimed.Add((await _guarded.Get(_observerId), await _guarded.GetRunningStateChangedAt(_observerId)));
            await _observer.SetHandledStats(99UL);
            _reclaimed.Add((await _guarded.Get(_observerId), await _guarded.GetRunningStateChangedAt(_observerId)));
        }
    }

    [Fact] void should_claim_without_changing_the_known_timestamp() => _claimed[0].ChangedAt.ShouldEqual(_entryTime);
    [Fact] void should_claim_without_inventing_a_timestamp() => _claimed[1].ChangedAt.ShouldBeNull();
    [Fact] void should_persist_the_timestamp_after_reclaiming_and_retrying_the_write() => _reclaimed[0].ChangedAt.ShouldEqual(_entryTime);
    [Fact] void should_persist_null_after_reclaiming_and_retrying_the_write() => _reclaimed[1].ChangedAt.ShouldBeNull();
    [Fact] void should_change_only_the_owner_on_claim() => _claimed.ShouldEachConformTo(_ => _.State.Owner == (EventSequenceId)"another-sequence" && _.State.RunningState == ObserverRunningState.Active);
    [Fact] void should_reclaim_for_the_stored_definition() => _reclaimed.ShouldEachConformTo(_ => _.State.Owner == _observerKey.EventSequenceId && _.State.RunningState == ObserverRunningState.Active);
    [Fact] void should_persist_progress_after_each_retry() => _reclaimed.ShouldEachConformTo(_ => _.State.LastHandledEventSequenceNumber == (Concepts.Events.EventSequenceNumber)99UL);
}
