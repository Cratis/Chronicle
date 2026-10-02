// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_snapshotting_capture_storage : an_event_sequence_with_a_capture_observer
{
    protected ObserverState _persistedCaptureState;
    protected bool _failNextCaptureEntryWrite;

    void Establish()
    {
        _persistedCaptureState = Snapshot(_captureState.State);
        _captureState.ReadStateAsync().Returns(_ =>
        {
            _captureState.State = Snapshot(_persistedCaptureState);
            return Task.CompletedTask;
        });
        _captureState.WriteStateAsync().Returns(_ =>
        {
            if (_failNextCaptureEntryWrite && _captureState.State.RunningState == ObserverRunningState.Unknown)
            {
                _failNextCaptureEntryWrite = false;
                return Task.FromException(new TimeoutException());
            }

            _persistedCaptureState = Snapshot(_captureState.State);
            return Task.CompletedTask;
        });
    }

    static ObserverState Snapshot(ObserverState state) => state with
    {
        ReplayingPartitions = new HashSet<Concepts.Keys.Key>(state.ReplayingPartitions),
        CatchingUpPartitions = new HashSet<Concepts.Keys.Key>(state.CatchingUpPartitions),
        InFlightPartitions = new HashSet<Concepts.Keys.Key>(state.InFlightPartitions),
        HandledEventCountPerEventType = state.HandledEventCountPerEventType.ToDictionary(),
        FailedPartitions = state.FailedPartitions.Select(partition => new FailedPartition
        {
            Id = partition.Id,
            Partition = partition.Partition,
            ObserverId = partition.ObserverId,
            Attempts = partition.Attempts.ToArray(),
            IsResolved = partition.IsResolved,
            IsQuarantined = partition.IsQuarantined
        }).ToArray()
    };
}
