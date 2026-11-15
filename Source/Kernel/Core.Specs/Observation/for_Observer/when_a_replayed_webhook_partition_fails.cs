// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_replayed_webhook_partition_fails : given.a_webhook_observer
{
    static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000005"));

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["503"], string.Empty, FailureKind.Handling, null, null, new PartitionFailureContext(WebhookDeliveryFailurePolicy.Retry, null, _epoch));

    [Fact] void should_record_the_replay_epoch_on_the_failed_partition() => _failedPartitionsState.Partitions.Single().ReplayEpoch.ShouldEqual(_epoch);
}
