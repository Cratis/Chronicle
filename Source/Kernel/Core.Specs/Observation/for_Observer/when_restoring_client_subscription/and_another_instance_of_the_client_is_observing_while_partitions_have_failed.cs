// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

/// <summary>
/// A restored instance may carry the fix for a failed partition, so the full reconcile retries the failures.
/// </summary>
public class and_another_instance_of_the_client_is_observing_while_partitions_have_failed : given.an_observer_restoring_another_client_instance
{
    void Establish()
    {
        var failures = new FailedPartitions();
        failures.AddFailedPartition("some-event-source");
        _failedPartitionsStorage.State = failures;
    }

    Task Because() => RestoreSecondClient();

    [Fact] void should_run_the_full_reconcile() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
