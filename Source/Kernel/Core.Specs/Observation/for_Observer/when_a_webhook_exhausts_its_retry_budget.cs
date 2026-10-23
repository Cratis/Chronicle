// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_exhausts_its_retry_budget : given.a_webhook_observer
{
    protected override Configuration.Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, MaxRetryAttempts = 1 };

    async Task Establish() => await _observer.PartitionFailed("partition", 42UL, ["503"], "");

    async Task Because()
    {
        var failure = _failedPartitionsState.Partitions.Single();
        await _observer.PartitionFailed("partition", 42UL, ["503 again"], "", FailureKind.Handling, failure.Id, failure.Deferral!.Id);
    }

    [Fact] void should_park_when_the_budget_is_exhausted() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeTrue();
    [Fact] void should_consume_exactly_the_real_attempts() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(2);
    [Fact] void should_not_keep_an_expired_deferral() => _failedPartitionsState.Partitions.Single().Deferral.ShouldBeNull();
}
