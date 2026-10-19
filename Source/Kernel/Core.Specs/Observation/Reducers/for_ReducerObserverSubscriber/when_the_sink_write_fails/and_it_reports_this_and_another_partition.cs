// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

using FailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_the_sink_write_fails;

/// <summary>
/// A bulk flush that dropped this partition's write along with another's: the other partition is recorded as failed so
/// it is retried, and this one fails through its own result rather than being reported twice.
/// </summary>
public class and_it_reports_this_and_another_partition : given.a_subscriber_whose_sink_write_fails
{
    readonly FailedPartition _own = new(Partition, 42UL) { Reason = "write rejected" };

    protected override IReadOnlyCollection<FailedPartition> FailedPartitions => [_own, _other];

    async Task Because() => await Deliver();

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_record_the_other_partition_as_failed() =>
        _observer.Received(1).PartitionFailed(_other.EventSourceId, _other.EventSequenceNumber, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
    [Fact] void should_not_record_this_partition_through_the_observer() =>
        _observer.DidNotReceive().PartitionFailed(Arg.Is<Key>(key => key.Value.Equals(Partition)), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>());
}
