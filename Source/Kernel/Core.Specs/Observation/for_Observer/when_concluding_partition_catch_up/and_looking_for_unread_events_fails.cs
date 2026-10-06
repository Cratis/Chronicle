// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_concluding_partition_catch_up;

/// <summary>
/// A failed lookup cannot tell whether an event was missed, so the partition is neither handed back over events nobody
/// may have read nor kept spinning: it is failed from where its step got to, and recovered from there.
/// </summary>
public class and_looking_for_unread_events_fails : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "partition";
    static readonly EventSequenceNumber _nextUnread = 10UL;

    bool _concluded;

    void Establish()
    {
        _stateStorage.State.CatchingUpPartitions.Add(_partition);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(_nextUnread, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
            .Returns(GetSequenceNumberError.StorageError);
    }

    async Task Because() => _concluded = await _observer.ConcludePartitionCatchUp(_partition, _nextUnread, [event_type]);

    [Fact] void should_stop_the_step_from_reading_on() => _concluded.ShouldBeTrue();
    [Fact] void should_fail_the_partition_from_where_its_step_got_to() => _failedPartitionsState.Partitions.Single(_ => _.Partition == _partition).Attempts.Last().SequenceNumber.ShouldEqual(_nextUnread);
    [Fact] void should_no_longer_hold_back_the_partition_as_catching_up() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(_partition);
}
