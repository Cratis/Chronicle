// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_preparing_steps;

public class and_only_a_few_of_many_partitions_failed : given.a_replay_observer_job
{
    static readonly Key _failed = "partition-100";
    static readonly Key _anotherFailed = "partition-900";
    int _stepCount;

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reducer };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(Enumerable.Range(0, 1000).Select(_ => (Key)$"partition-{_}").ToArray()));
        _observer.GetFailedPartitionKeys().Returns([_failed, _anotherFailed]);
    }

    async Task Because()
    {
        await _job.Start(_request);
        _stepCount = (await _job.PrepareStepsForTesting(_request)).Count;
    }

    [Fact] void should_still_replay_every_partition() => _stepCount.ShouldEqual(1000);
    [Fact] void should_only_store_failed_partition_steps() => _stateStorage.State.ReplayPartitionSteps.Select(_ => _.Partition).ShouldContain(_failed);
    [Fact] void should_only_store_two_step_watermarks() => _stateStorage.State.ReplayPartitionSteps.Count.ShouldEqual(2);
}
