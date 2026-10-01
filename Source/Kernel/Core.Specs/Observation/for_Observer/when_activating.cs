// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_activating : given.an_observer_with_metrics
{
    [Fact] void should_not_record_failed_partitions() => _metrics.For(PartitionsFailed).ShouldBeEmpty();
    [Fact] void should_not_record_partition_retry_attempts() => _metrics.For(PartitionRetryAttempts).ShouldBeEmpty();
    [Fact] void should_not_record_quarantined_partitions() => _metrics.For(PartitionsQuarantined).ShouldBeEmpty();
    [Fact] void should_not_record_quarantined_observers() => _metrics.For(ObserverQuarantined).ShouldBeEmpty();
}
