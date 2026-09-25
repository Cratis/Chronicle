// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_one_partition_handled_nothing : given.a_replay_observer_job
{
    static readonly Key _failedPartition = "failed-partition";
    static readonly Key _otherPartition = "other-partition";
    static readonly EventType _replayedType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor, EventTypes = [_replayedType] };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_failedPartition, _otherPartition));
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        var steps = await _job.PrepareStepsForTesting(_request);
        await _job.RecordStepForTesting(steps[0].Id, JobStepResult.Succeeded(new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)));
        await _job.RecordStepForTesting(steps[1].Id, JobStepResult.Succeeded(new HandleEventsForPartitionResult(42UL)));
        await _job.CompleteForTesting();
    }

    [Fact] void should_only_report_the_partition_that_handled_events() => _observer.Received(1).ReplayedSuccessfully(
        42UL,
        Arg.Is<IReadOnlyDictionary<Key, EventSequenceNumber>>(_ => _.Count == 1 && _.ContainsKey(_otherPartition) && !_.ContainsKey(_failedPartition)),
        Arg.Is<EventType[]>(_ => _.Length == 1 && _[0] == _replayedType));
}
