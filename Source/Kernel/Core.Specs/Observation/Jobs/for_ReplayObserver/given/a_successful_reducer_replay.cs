// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.given;

public class a_successful_reducer_replay : a_replay_observer_job
{
    protected ReplayContext _context;
    void Establish()
    {
        _stateStorage.State.Request = _request with { ObserverType = ObserverType.Reducer };
        _stateStorage.State.Status = JobStatus.Running;
        _stateStorage.State.Progress.TotalSteps = 1;
        _stateStorage.State.Progress.SuccessfulSteps = 1;
        _stateStorage.State.LastHandledEventSequenceNumber = 42UL;
        _stateStorage.State.HandledAllEvents = true;
        _context = new(new("model", 1), "Model", "Revert", DateTimeOffset.UtcNow) { ReplayContainerName = "isolated" };
        _stateStorage.State.ReducerReplayContext = _context;
        _reducerReplay.Publish(Arg.Any<ReplayContext>()).Returns(ReplayPublication.Published);
    }
}
