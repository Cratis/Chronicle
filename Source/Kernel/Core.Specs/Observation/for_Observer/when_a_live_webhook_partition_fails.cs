// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_live_webhook_partition_fails : given.a_webhook_observer
{
    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["503"], string.Empty);

    [Fact] void should_record_no_replay_epoch() => _failedPartitionsState.Partitions.Single().ReplayEpoch.ShouldBeNull();
}
