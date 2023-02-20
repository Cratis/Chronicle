// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.for_FailedPartitionSupervisor.when_resetting;

public class and_there_are_no_failed_partitions : given.a_supervisor
{
    FailedPartitionSupervisor supervisor;

    void Establish() => supervisor = get_clean_supervisor();

    async Task Because() => await supervisor.Reset();

    [Fact] void should_not_get_any_recovery_grains() => grain_factory.Verify(_ => _.GetGrain<IRecoverFailedPartition>(IsAny<Guid>(), IsAny<string>(), null), Never);
}
