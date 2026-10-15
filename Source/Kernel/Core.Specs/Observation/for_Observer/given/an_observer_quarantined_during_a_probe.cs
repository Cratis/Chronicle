// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_quarantined_during_a_probe : an_observer_with_subscription
{
    protected readonly TaskCompletionSource _probeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly Key _partition = "recovered-partition";

    protected override Observers CreateObserversConfig() => new() { QuarantineOnFailedPartitionCount = 1 };

    void Establish()
    {
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(43UL, Arg.Any<IEnumerable<EventType>>(), (EventSourceId)_partition.ToString())
            .Returns(Result<EventSequenceNumber, GetSequenceNumberError>.Failed(GetSequenceNumberError.StorageError));
        _appendedEventsQueues.ClearReceivedCalls();
    }

    protected async Task QuarantineDuringProbe(Task operation, Action releaseProbe)
    {
        try
        {
            await _probeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.FailedPartitionRecovered(_partition, 42UL);
        }
        finally
        {
            releaseProbe();
        }

        await operation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }
}
