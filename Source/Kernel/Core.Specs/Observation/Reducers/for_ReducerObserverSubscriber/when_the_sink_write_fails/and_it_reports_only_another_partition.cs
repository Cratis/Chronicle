// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_the_sink_write_fails;

/// <summary>
/// A bulk flush that dropped only another partition's write: that partition is recorded as failed, and this
/// partition's result stands.
/// </summary>
public class and_it_reports_only_another_partition : given.a_subscriber_whose_sink_write_fails
{
    async Task Because() => await Deliver();

    [Fact] void should_keep_this_partitions_result() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_record_the_other_partition_as_failed() =>
        _observer.Received(1).PartitionFailed(_other.EventSourceId, _other.EventSequenceNumber, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
    [Fact] void should_not_record_this_partition_as_failed() =>
        _observer.DidNotReceive().PartitionFailed(Arg.Is<Key>(key => key.Value.Equals(Partition)), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>());
}
