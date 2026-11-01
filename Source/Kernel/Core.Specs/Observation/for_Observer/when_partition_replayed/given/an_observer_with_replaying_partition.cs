// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.for_Observer.given;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.given;

public class an_observer_with_replaying_partition : an_observer
{
    protected static Key _partition;
    protected static EventSequenceNumber _lastHandledEventSequenceNumber;
    protected static EventSequenceNumber _nextEventSequenceNumber;

    void Establish()
    {
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _partition = "some-partition";
        _lastHandledEventSequenceNumber = 42UL;
        _nextEventSequenceNumber = 45UL;
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = _lastHandledEventSequenceNumber,
            NextEventSequenceNumber = _nextEventSequenceNumber,
            ReplayingPartitions = new HashSet<Key>([_partition])
        };
    }

    protected void CheckStartedCatchupJob(EventSequenceNumber lastHandled) => CheckStartedCatchupJob(lastHandled, _partition);
}
