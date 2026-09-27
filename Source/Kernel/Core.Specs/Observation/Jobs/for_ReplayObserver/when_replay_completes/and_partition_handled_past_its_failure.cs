// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_partition_handled_past_its_failure : given.a_replay_observer_job
{
    static readonly Key _partition = "failed-partition";
    static readonly EventType _replayedType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reducer, EventTypes = [_replayedType] };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_partition));
        _observer.GetFailedPartitionKeys().Returns([_partition]);
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        var steps = await _job.PrepareStepsForTesting(_request);
        await _job.RecordStepForTesting(steps[0].Id, JobStepResult.Succeeded(new HandleEventsForPartitionResult(42UL)));
        await _job.CompleteForTesting();
    }

    [Fact] void should_report_the_partitions_own_watermark() => _observer.Received(1).ReplayedSuccessfullySince(
        42UL,
        Arg.Is<IReadOnlyDictionary<Key, EventSequenceNumber>>(_ => _.Count == 1 && _[_partition] == (EventSequenceNumber)42UL),
        Arg.Is<EventType[]>(_ => _.Length == 1 && _[0] == _replayedType),
        Arg.Any<DateTimeOffset>());
}
