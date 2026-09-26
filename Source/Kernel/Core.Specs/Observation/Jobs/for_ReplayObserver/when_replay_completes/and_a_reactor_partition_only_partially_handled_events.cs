// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_replay_completes;

public class and_a_reactor_partition_only_partially_handled_events : given.a_replay_observer_job
{
    static readonly Key _partition = "failed-partition";
    static readonly EventType _replayedType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor, EventTypes = [_replayedType] };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys(_partition));
        _observer.GetFailedPartitionKeys().Returns([_partition]);
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
    }

    async Task Because()
    {
        await _job.Start(_request);
        var steps = await _job.PrepareStepsForTesting(_request);
        await _job.RecordStepForTesting(steps[0].Id, JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(new HandleEventsForPartitionResult(42UL), ["failed"], "trace")));
        await _job.CompleteForTesting();
    }

    [Fact] void should_not_report_the_partition_as_covered() => _observer.Received(1).ReplayedSuccessfullySince(
        42UL,
        Arg.Is<IReadOnlyDictionary<Key, EventSequenceNumber>>(_ => _.Count == 0),
        Arg.Any<EventType[]>(),
        Arg.Any<DateTimeOffset>());
}
