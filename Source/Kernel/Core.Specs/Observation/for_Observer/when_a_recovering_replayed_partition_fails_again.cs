// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_recovering_replayed_partition_fails_again : given.a_webhook_observer
{
    static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000007"));

    async Task Establish() => await _observer.PartitionFailed("partition", 42UL, ["503"], string.Empty, FailureKind.Handling, null, null, new PartitionFailureContext(WebhookDeliveryFailurePolicy.Retry, null, _epoch));

    async Task Because()
    {
        var failure = _failedPartitionsState.Partitions.Single();
        await _observer.PartitionFailed("partition", 42UL, ["503 again"], string.Empty, FailureKind.Handling, failure.Id, failure.Deferral!.Id);
    }

    [Fact] void should_have_recorded_the_second_attempt() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(2);
    [Fact] void should_keep_the_replay_epoch() => _failedPartitionsState.Partitions.Single().ReplayEpoch.ShouldEqual(_epoch);
}
