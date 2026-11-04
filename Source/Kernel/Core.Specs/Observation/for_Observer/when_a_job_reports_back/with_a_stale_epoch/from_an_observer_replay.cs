// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_job_reports_back.with_a_stale_epoch;

public class from_an_observer_replay : given.an_observer_holding_a_newer_claim
{
    Task Establish() => HoldObserverReplay();

    async Task Because()
    {
        await _observer.ReplayedSuccessfullySince(44UL, new Dictionary<Key, EventSequenceNumber>(), [], DateTimeOffset.UtcNow, StaleEpoch);
        await _observer.Replayed(44UL, StaleEpoch);
    }

    [Fact] void should_keep_replaying() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_keep_the_newer_claim() => _stateStorage.State.ObserverWideClaim!.Epoch.ShouldEqual(new ClaimEpoch(CurrentEpoch));
    [Fact] void should_keep_the_handled_position() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_keep_admitting_the_newer_job() => AdmitsNewerObserverReplay().ShouldBeTrue();
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
