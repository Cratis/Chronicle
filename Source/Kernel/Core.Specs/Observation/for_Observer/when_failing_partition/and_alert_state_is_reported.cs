// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_alert_state_is_reported : given.an_observer
{
    ObserverAlertSnapshot _snapshot;

    void Establish() => _observerAlerts.Reconcile(Arg.Do<ObserverAlertSnapshot>(snapshot => _snapshot = snapshot));

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["Failed", "Inner message"], "Stack", FailureKind.Handling);
        await ReportAlerts();
    }

    [Fact] void should_report_the_observer_key() => _snapshot.Observer.ShouldEqual(_observerKey);
    [Fact] void should_use_the_persisted_failure_episode_identifier() => _snapshot.FailedPartitions.Single().Id.ShouldEqual(_failedPartitionsState.Partitions.Single().Id);
    [Fact] void should_report_the_partition() => _snapshot.FailedPartitions.Single().Partition.Value.ShouldEqual("partition");
    [Fact] void should_report_the_attempt_count() => _snapshot.FailedPartitions.Single().AttemptCount.ShouldEqual(1);
    [Fact] void should_report_the_first_attempt_time() => _snapshot.FailedPartitions.Single().FirstAttempt.ShouldEqual(_failedPartitionsState.Partitions.Single().Attempts.First().Occurred);
    [Fact] void should_report_the_latest_failure_kind() => _snapshot.FailedPartitions.Single().FailureKind.ShouldEqual(FailureKind.Handling);
    [Fact] void should_report_only_the_first_message() => _snapshot.FailedPartitions.Single().Message.ShouldEqual("Failed");
    [Fact] void should_report_the_configured_retry_limit() => _snapshot.MaxRetryAttempts.ShouldEqual(_observersConfig.MaxRetryAttempts);
}
