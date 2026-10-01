// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class with_metrics : given.an_observer_with_metrics
{
    async Task Because() => await _observer.PartitionFailed("Something", 42UL, ["Something went wrong"], "This is the stack trace");

    [Fact] void should_count_the_failed_attempt() => _metrics.SumOf(PartitionsFailed).ShouldEqual(1);
    [Fact] void should_count_the_retry_attempt() => _metrics.SumOf(PartitionRetryAttempts).ShouldEqual(1);
    [Fact] void should_not_count_a_quarantined_partition() => _metrics.SumOf(PartitionsQuarantined).ShouldEqual(0);
    [Fact] void should_not_count_a_quarantined_observer() => _metrics.SumOf(ObserverQuarantined).ShouldEqual(0);
    [Fact] void should_not_tag_any_failure_instrument_with_the_partition() => _failureInstruments.Any(instrument => _metrics.For(instrument).Any(_ => _.Tags.ContainsKey("partition"))).ShouldBeFalse();
    [Fact] void should_tag_every_failure_instrument_with_the_observer_scope_only() => _failureInstruments.All(instrument => _metrics.For(instrument).All(_ => _.Tags.Keys.Order().SequenceEqual(_observerScopeTags.Order()))).ShouldBeTrue();
}
