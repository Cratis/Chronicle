// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_gauges;

public class and_the_observer_is_quarantined : given.an_observer_reporting_gauges
{
    Task Because() => _observer.TransitionTo<QuarantinedObserver>();

    [Fact] void should_register_the_observer_once() => _registered.Count.ShouldEqual(1);
    [Fact] void should_register_it_under_its_key() => Entry!.ObserverKey.ShouldEqual(_observerKey);
    [Fact] void should_report_the_quarantined_running_state() => Entry!.Snapshot.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_release_it() => _released.ShouldBeEmpty();
}
