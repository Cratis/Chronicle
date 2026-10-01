// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class with_metrics : given.an_observer_with_metrics
{
    async Task Because() => await _observer.PartitionFailed("Something", 42UL, ["Something went wrong"], "This is the stack trace");

    [Fact] void should_count_the_failed_attempt() => _metrics.SumOf(PartitionsFailed).ShouldEqual(1);
    [Fact] void should_count_the_retry_attempt() => _metrics.SumOf(PartitionRetryAttempts).ShouldEqual(1);
    [Fact] void should_not_count_a_quarantined_partition() => _metrics.For(PartitionsQuarantined).ShouldBeEmpty();
    [Fact] void should_not_count_a_quarantined_observer() => _metrics.For(ObserverQuarantined).ShouldBeEmpty();
    [Fact] void should_not_tag_the_failed_attempt_with_the_partition() => IsNotTaggedWithPartition(PartitionsFailed).ShouldBeTrue();
    [Fact] void should_not_tag_the_retry_attempt_with_the_partition() => IsNotTaggedWithPartition(PartitionRetryAttempts).ShouldBeTrue();
    [Fact] void should_tag_the_failed_attempt_with_the_observer_scope_only() => IsTaggedWithObserverScopeOnly(PartitionsFailed).ShouldBeTrue();
    [Fact] void should_tag_the_retry_attempt_with_the_observer_scope_only() => IsTaggedWithObserverScopeOnly(PartitionRetryAttempts).ShouldBeTrue();
    [Fact] void should_create_every_failure_instrument_without_a_unit() => _failureInstruments.All(instrument => _metrics.InstrumentNamed(instrument) is { Unit: null }).ShouldBeTrue();
    [Fact] void should_create_every_failure_instrument_with_a_description() => _failureInstruments.All(instrument => !string.IsNullOrWhiteSpace(_metrics.InstrumentNamed(instrument)?.Description)).ShouldBeTrue();
}
