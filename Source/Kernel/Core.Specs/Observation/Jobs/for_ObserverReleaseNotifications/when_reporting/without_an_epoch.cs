// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverReleaseNotifications.when_reporting;

public class without_an_epoch : Specification
{
    static readonly Key _partition = "partition";
    static readonly DateTimeOffset _startedAt = DateTimeOffset.UnixEpoch;
    IObserver _observer;
    ReplayObserverPartitionRequest _partitionRequest;
    ReplayObserverRequest _replayRequest;
    IReadOnlyDictionary<Key, EventSequenceNumber> _covered;
    EventType[] _eventTypes;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _partitionRequest = new(ObserverKey.NotSet, ObserverType.Reactor, _partition, 1UL, EventSequenceNumber.Max, []);
        _replayRequest = new(ObserverKey.NotSet, ObserverType.Reactor, []);
        _covered = new Dictionary<Key, EventSequenceNumber>();
        _eventTypes = [];
    }

    async Task Because()
    {
        await _observer.PartitionCaughtUp(_partitionRequest, _partition, 3UL, 4UL);
        await _observer.PartitionReplayed(_partitionRequest, _partition, 3UL, 4UL, _eventTypes);
        await _observer.ReleaseRefusedPartitionReplay(_partitionRequest, _partition, 5UL);
        await _observer.Replayed(_replayRequest, 3UL);
        await _observer.ReplayedSuccessfullySince(_replayRequest, 3UL, _covered, _eventTypes, _startedAt);
        await _observer.ReleaseRefusedReplay(_replayRequest);
    }

    [Fact] void should_report_the_partition_catch_up() => _observer.Received(1).PartitionCaughtUp(_partition, 3UL, 4UL);
    [Fact] void should_report_the_partition_replay() => _observer.Received(1).PartitionReplayed(_partition, 3UL, 4UL, _eventTypes);
    [Fact] void should_release_the_refused_partition_replay() => _observer.Received(1).ReleaseRefusedPartitionReplay(_partition, 5UL);
    [Fact] void should_report_the_replay() => _observer.Received(1).Replayed(3UL);
    [Fact] void should_report_the_successful_replay() => _observer.Received(1).ReplayedSuccessfullySince(3UL, _covered, _eventTypes, _startedAt);
    [Fact] void should_release_the_refused_replay() => _observer.Received(1).ReleaseRefusedReplay();
}
