// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_activating : given.an_observer_with_metrics
{
    [Fact] void should_record_zero_failed_partitions() => _metrics.For(PartitionsFailed).Select(_ => _.Value).ShouldContainOnly(0);
    [Fact] void should_record_zero_partition_retry_attempts() => _metrics.For(PartitionRetryAttempts).Select(_ => _.Value).ShouldContainOnly(0);
    [Fact] void should_record_zero_quarantined_partitions() => _metrics.For(PartitionsQuarantined).Select(_ => _.Value).ShouldContainOnly(0);
    [Fact] void should_record_zero_quarantined_observers() => _metrics.For(ObserverQuarantined).Select(_ => _.Value).ShouldContainOnly(0);
    [Fact] void should_tag_the_zeros_with_the_observer_scope_only() => _failureInstruments.All(instrument => _metrics.For(instrument).All(_ => _.Tags.Keys.Order().SequenceEqual(_observerScopeTags.Order()))).ShouldBeTrue();
}
