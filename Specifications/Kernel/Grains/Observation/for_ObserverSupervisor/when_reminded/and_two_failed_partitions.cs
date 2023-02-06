// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverSupervisor.when_reminded;

public class and_two_failed_partitions : given.an_observer_with_event_types_a_reminder_and_two_failing_partitions
{
    void Establish() => event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_sequence_id, event_types, null)).Returns(Task.FromResult((EventSequenceNumber)83));

    async Task Because() => await observer.ReceiveReminder(ObserverSupervisor.RecoverReminder, new TickStatus());

    [Fact] void should_start_recovering_first_partition() => state.IsRecoveringPartition(first_partition).ShouldBeTrue();
    [Fact] void should_start_recovering_second_partition() => state.IsRecoveringPartition(second_partition).ShouldBeTrue();
}
