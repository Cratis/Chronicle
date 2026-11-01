// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_a_controlled_hold;

public class and_the_observer_is_not_controlled : given.an_observer_with_subscription_for_specific_event_type
{
    Task Because() => _observer.PartitionHeld((Key)"partition", new(10UL, FailedPartitionRecoveryMode.Live, null), event_type.Id);

    [Fact] void should_not_record_a_hold() => _failedPartitionsState.Partitions.ShouldBeEmpty();
    [Fact] void should_not_change_the_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
}
