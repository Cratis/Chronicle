// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// A new instance may carry the fix for a failed partition, so the full subscribe retries the failures.
/// </summary>
public class and_another_client_instance_subscribes_with_same_definition_while_partitions_have_failed : given.an_observer_served_by_one_client_instance
{
    void Establish()
    {
        var failures = new FailedPartitions();
        failures.AddFailedPartition("some-event-source");
        _failedPartitionsStorage.State = failures;
    }

    Task Because() => SubscribeSecondClient();

    [Fact] void should_run_the_full_subscribe() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
