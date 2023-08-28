// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_RecoverFailedPartition.given;

public class an_initiated_recover_failed_partition_worker : a_recover_failed_partition_worker
{
    protected override RecoverFailedPartitionState BuildState() => new()
    {
        ObserverId = ObserverId,
        SubscriberKey = subscriber_key,
        ObserverKey = ObserverKey
    };
}
