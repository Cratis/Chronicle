// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_alert_reporting_fails : given.an_observer
{
    Exception _error;

    void Establish() => _observerAlerts.Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(Task.FromException(new InvalidOperationException("Dispatch failed")));

    async Task Because() => _error = await Catch.Exception(() => _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack"));

    [Fact] void should_not_fail_observation() => _error.ShouldBeNull();
    [Fact] void should_still_record_the_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(1);
}
