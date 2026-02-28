// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.for_Observer.given;
namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_recovered.given;

public class all_dependencies : an_observer
{
    protected static Key _partition;
    protected static EventSequenceNumber _lastHandledEventSequenceNumber;
    protected static EventSequenceNumber _nextEventSequenceNumber;

    void Establish()
    {
        _partition = "some-partition";
        _lastHandledEventSequenceNumber = 42UL;
        _nextEventSequenceNumber = 45UL;
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = _lastHandledEventSequenceNumber,
            NextEventSequenceNumber = _nextEventSequenceNumber
        };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, 12UL);
    }

    protected void CheckStartedCatchupJob(EventSequenceNumber lastHandled) => CheckStartedCatchupJob(lastHandled, _partition);
}
