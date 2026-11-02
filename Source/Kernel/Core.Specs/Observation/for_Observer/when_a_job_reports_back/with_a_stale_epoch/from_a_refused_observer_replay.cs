// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_job_reports_back.with_a_stale_epoch;

public class from_a_refused_observer_replay : given.an_observer_holding_a_newer_claim
{
    Task Establish() => HoldObserverReplay();

    Task Because() => _observer.ReleaseRefusedReplay(StaleEpoch);

    [Fact] void should_keep_replaying() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_keep_the_newer_claim() => _stateStorage.State.ObserverWideClaim!.Epoch.ShouldEqual(new ClaimEpoch(CurrentEpoch));
    [Fact] void should_keep_admitting_the_newer_job() => AdmitsNewerObserverReplay().ShouldBeTrue();
}
